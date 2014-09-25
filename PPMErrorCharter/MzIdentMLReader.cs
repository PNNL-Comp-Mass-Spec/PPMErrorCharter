using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace PPMErrorCharter
{
	/// <summary>
	/// Read and perform some processing on a MZIdentML file
	/// Processes the data into an LCMS DataSet
	/// </summary>
	public static class MzIdentMLReader
	{
		/// <summary>
		/// Entry point for MZIdentMLReader, overriden from PHRPReaderBase
		/// Read the MZIdentML file, map the data to MSGF+ data, compute the NETs, and return the LCMS DataSet
		/// </summary>
		/// <param name="path">Path to *.mzid/mzIdentML file</param>
		/// <returns>List<ScanData></returns>
		public static List<IdentData> Read(string path)
		{
			var scanData = new List<IdentData>();

			do
			{
				scanData.Clear();
				// Set a large buffer size. Doesn't affect gzip reading speed, but speeds up non-gzipped
				Stream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);

				if (path.EndsWith(".mzid.gz"))
				{
					file = new GZipStream(file, CompressionMode.Decompress);
				}

				var xSettings = new XmlReaderSettings {IgnoreWhitespace = true};
				var reader = XmlReader.Create(new StreamReader(file, System.Text.Encoding.UTF8, true, 65536), xSettings);

				// Read in the file
				ReadMzIdentMl(reader, scanData);
			} while (scanData.Count < 500 && IdentData.AdjustThreshold());

			return scanData;
		}

		/// <summary>
		/// Read and parse a .mzid file, or mzIdentML
		/// Files are commonly larger than 30 MB, so use a streaming reader instead of a DOM reader
		/// </summary>
		/// <param name="reader">XmlReader object for the file to be read</param>
		/// <param name="scanData"></param>
		private static void ReadMzIdentMl(XmlReader reader, List<IdentData> scanData)
		{
			// Handle disposal of allocated object correctly
			using (reader)
			{
				// Guarantee a move to the root node
				reader.MoveToContent();
				// Consume the MzIdentML root tag
				// Throws exception if we are not at the "MzIdentML" tag.
				// This is a critical error; we want to stop processing for this file if we encounter this error
				reader.ReadStartElement("MzIdentML");
				// Read the next node - should be the first child node
				while (reader.ReadState == ReadState.Interactive)
				{
					// Handle exiting out properly at EndElement tags
					if (reader.NodeType != XmlNodeType.Element)
					{
						reader.Read();
						continue;
					}
					// Handle each 1st level as a chunk
					switch (reader.Name)
					{
						case "cvList":
							// Schema requirements: one instance of this element
							reader.Skip();
							break;
						case "AnalysisSoftwareList":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "Provider":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "AuditCollection":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "AnalysisSampleCollection":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "SequenceCollection":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "AnalysisCollection":
							// Schema requirements: one instance of this element
							reader.Skip();
							break;
						case "AnalysisProtocolCollection":
							// Schema requirements: one instance of this element
							reader.Skip();
							break;
						case "DataCollection":
							// Schema requirements: one instance of this element
							// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
							ReadDataCollection(reader.ReadSubtree(), scanData);
							reader.ReadEndElement(); // "DataCollection" must have child nodes
							break;
						case "BibliographicReference":
							// Schema requirements: zero to many instances of this element
							reader.Skip();
							break;
						default:
							// We are not reading anything out of the tag, so bypass it
							reader.Skip();
							break;
					}
				}
			}
		}

		/// <summary>
		/// Handle the child nodes of the DataCollection element
		/// Called by ReadMzIdentML (xml hierarchy)
		/// Currently we are only working with the AnalysisData child element
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single DataCollection element</param>
		/// <param name="scanData"></param>
		private static void ReadDataCollection(XmlReader reader, List<IdentData> scanData)
		{
			reader.MoveToContent();
			reader.ReadStartElement("DataCollection"); // Throws exception if we are not at the "DataCollection" tag.
			while (reader.ReadState == ReadState.Interactive)
			{
				// Handle exiting out properly at EndElement tags
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}
				if (reader.Name == "AnalysisData")
				{
					// Schema requirements: one and only one instance of this element
					// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
					ReadAnalysisData(reader.ReadSubtree(), scanData);
					reader.ReadEndElement(); // "AnalysisData" must have child nodes
				}
				else
				{
					reader.Skip();
				}
			}
			reader.Close();
		}

		/// <summary>
		/// Handle child nodes of AnalysisData element
		/// Called by ReadDataCollection (xml hierarchy)
		/// Currently we are only working with the SpectrumIdentificationList child elements
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single AnalysisData element</param>
		/// <param name="scanData"></param>
		private static void ReadAnalysisData(XmlReader reader, List<IdentData> scanData)
		{
			reader.MoveToContent();
			reader.ReadStartElement("AnalysisData"); // Throws exception if we are not at the "AnalysisData" tag.
			while (reader.ReadState == ReadState.Interactive)
			{
				// Handle exiting out properly at EndElement tags
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}
				if (reader.Name == "SpectrumIdentificationList")
				{
					// Schema requirements: one to many instances of this element
					// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
					ReadSpectrumIdentificationList(reader.ReadSubtree(), scanData);
					reader.ReadEndElement(); // "SpectrumIdentificationList" must have child nodes
				}
				else
				{
					reader.Skip();
				}
			}
			reader.Close();
		}

		/// <summary>
		/// Handle the child nodes of a SpectrumIdentificationList element
		/// Called by ReadAnalysisData (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single SpectrumIdentificationList element</param>
		/// <param name="scanData"></param>
		private static void ReadSpectrumIdentificationList(XmlReader reader, List<IdentData> scanData)
		{
			reader.MoveToContent();
			reader.ReadStartElement("SpectrumIdentificationList"); // Throws exception if we are not at the "SpectrumIdentificationList" tag.
			while (reader.ReadState == ReadState.Interactive)
			{
				// Handle exiting out properly at EndElement tags
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}
				if (reader.Name == "SpectrumIdentificationResult")
				{
					// Schema requirements: one to many instances of this element
					// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
					ReadSpectrumIdentificationResult(reader.ReadSubtree(), scanData);
					reader.ReadEndElement(); // "SpectrumIdentificationResult" must have child nodes
				}
				else
				{
					reader.Skip();
				}
			}
			reader.Close();
		}

		/// <summary>
		/// Handle a single SpectrumIdentificationResult element and child nodes
		/// Called by ReadSpectrumIdentificationList (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single SpectrumIdentificationResult element</param>
		/// <param name="scanData"></param>
		private static void ReadSpectrumIdentificationResult(XmlReader reader, List<IdentData> scanData)
		{
			reader.MoveToContent();
			var nativeId = reader.GetAttribute("spectrumID");
			reader.ReadStartElement("SpectrumIdentificationResult"); // Throws exception if we are not at the "SpectrumIdentificationResult" tag.
			List<IdentData> newSpectra = new List<IdentData>();
			while (reader.ReadState == ReadState.Interactive)
			{
				// Handle exiting out properly at EndElement tags
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}

				switch (reader.Name)
				{
					case "SpectrumIdentificationItem":
						// Schema requirements: one to many instances of this element
						ReadSpectrumIdentificationItem(reader.ReadSubtree(), newSpectra, nativeId);
						reader.ReadEndElement(); // "SpectrumIdentificationItem" must have child nodes
						break;
					case "cvParam":
						// Schema requirements: zero to many instances of this element
						if (reader.GetAttribute("accession") == "MS:1001115")
						{
								ulong value = Convert.ToUInt64(reader.GetAttribute("value"));
								foreach (var item in newSpectra)
								{
									if (item.ScanId == 0)
									{
										item.ScanId = value;
									}
								}
						}
						//reader.Read(); // Consume the cvParam element (no child nodes)
						reader.Skip();
						break;
					case "userParam":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					default:
						reader.Skip();
						break;
				}
			}
			scanData.AddRange(newSpectra);
			reader.Close();
		}

		/// <summary>
		/// Handle a single SpectrumIdentificationItem element and child nodes
		/// Called by ReadSpectrumIdentificationResult (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single SpectrumIdentificationItem element</param>
		/// <param name="scanData"></param>
		private static void ReadSpectrumIdentificationItem(XmlReader reader, List<IdentData> scanData, string nativeId)
		{
			var data = new IdentData();

			reader.MoveToContent(); // Move to the "SpectrumIdentificationItem" element

			data.NativeId = nativeId;
			if (!string.IsNullOrWhiteSpace(nativeId))
			{
				if (data.NativeId.LastIndexOf("scan=") != -1)
				{
					data.IdField = "id";
					data.IdValue = data.NativeId;
					data.ScanId = Convert.ToUInt64(data.NativeId.Substring(data.NativeId.LastIndexOf("scan=") + 5));
				}
				else if (data.NativeId.LastIndexOf("index=") != -1)
				{
					data.IdField = "index";
					data.IdValue = data.NativeId.Substring(data.NativeId.LastIndexOf("index=") + 6);
					data.ScanId = Convert.ToUInt64(data.IdValue);
					//data.IdValue = (data.ScanId + 1).ToString();
				}
			}
			data.CalcMz = Convert.ToDouble(reader.GetAttribute("calculatedMassToCharge"));
			data.ExperMz = Convert.ToDouble(reader.GetAttribute("experimentalMassToCharge"));
			data.Charge = Convert.ToInt32(reader.GetAttribute("chargeState"));

			//data.MassError = data.ExperMz - data.CalcMz;
			//data.PpmError = (data.MassError / data.CalcMz) * 1.0e6;

			reader.ReadStartElement("SpectrumIdentificationItem"); // Throws exception if we are not at the "SpectrumIdentificationItem" tag.
			
			reader.ReadToNextSibling("cvParam");
			// Parse all of the cvParam/userParam fields
			while (reader.Name == "cvParam" || reader.Name == "userParam")
			{
				switch (reader.GetAttribute("name"))
				{
					case "MS-GF:RawScore":
						//specItem.RawScore = Convert.ToInt32(reader.GetAttribute("value"));
						break;
					case "MS-GF:DeNovoScore":
						//specItem.DeNovoScore = Convert.ToInt32(reader.GetAttribute("value"));
						break;
					case "MS-GF:SpecEValue":
						data.SpecEValue = Convert.ToDouble(reader.GetAttribute("value"));
						break;
					case "MS-GF:EValue":
						//specItem.EValue = Convert.ToDouble(reader.GetAttribute("value"));
						break;
					case "MS-GF:QValue":
						data.QValue = Convert.ToDouble(reader.GetAttribute("value"));
						break;
					case "MS-GF:PepQValue":
						//specItem.PepQValue = Convert.ToDouble(reader.GetAttribute("value"));
						break;
					case "IsotopeError":
						// userParam field
						//specItem.IsoError = Convert.ToInt32(reader.GetAttribute("value"));
						break;
					case "AssumedDissociationMethod":
						// userParam field
						break;
				}
				reader.Read();
			}
			if (data.SpecEValue < 1.0e-10 && 
				(-IdentData.IsotopeErrorFilterWindow < data.MassError && data.MassError < IdentData.IsotopeErrorFilterWindow) 
				&& (-IdentData.PpmErrorFilterWindow < data.PpmError && data.PpmError < IdentData.PpmErrorFilterWindow))
			{
				scanData.Add(data);
			}

			reader.Close();
		}
	}
}
