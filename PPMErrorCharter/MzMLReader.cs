using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace PPMErrorCharter
{
	public static class MzMLReader
	{
		private class ScanData
		{
			public double MonoisotopicMz;
			public double StartTime;

			public ScanData()
			{
				MonoisotopicMz = 0.0;
				StartTime = 0.0;
			}
		}

		private class SelectedIon
		{
			public double SelectedIonMz;
			public int Charge;

			public SelectedIon()
			{
				SelectedIonMz = 0.0;
				Charge = 0;
			}
		}

		private class Precursor
		{
			public List<SelectedIon> Ions;
			public double IsolationWindowTargetMz;
			public double IsolationWindowLowerOffset;
			public double IsolationWindowUpperOffset;

			public Precursor()
			{
				Ions = new List<SelectedIon>();
				IsolationWindowTargetMz = 0.0;
				IsolationWindowLowerOffset = 0.0;
				IsolationWindowUpperOffset = 0.0;
			}
		}

		/// <summary>
		/// Read and parse a .mzML file
		/// Files are commonly larger than 100 MB, so use a streaming reader instead of a DOM reader
		/// </summary>
		/// <param name="filePath">Path to mzML file</param>
		/// <param name="data">IdentData list</param>
		public static void ReadMzMl(string filePath, List<IdentData> data)
		{
			// Make sure it is sorted by ScanId
			data.Sort();
			int dataIndex = 0;

			// Set a very large read buffer, it does decrease the read times for uncompressed files.
			Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);

			if (filePath.EndsWith(".mzML.gz"))
			{
				file = new GZipStream(file, CompressionMode.Decompress);
			}

			var xSettings = new XmlReaderSettings { IgnoreWhitespace = true };
			var xmlReader = XmlReader.Create(new StreamReader(file, System.Text.Encoding.UTF8, true, 65536), xSettings);

			// Handle disposal of allocated object correctly
			using (xmlReader)
			{
				// Create a separate link to the xml reader to remove a warning, 
				//    and to not modify the one used by the using() statement
				var reader = xmlReader;
				// Guarantee a move to the root node
				reader.MoveToContent();

				if (reader.Name == "indexedmzML")
				{
					// Read to the mzML root tag, and ignore the extra indexedmzML data
					reader.ReadToDescendant("mzML");
					reader = reader.ReadSubtree();
					reader.MoveToContent();
				}
				// Consume the mzML root tag
				// Throws exception if we are not at the "mzML" tag.
				// This is a critical error; we want to stop processing for this file if we encounter this error
				reader.ReadStartElement("mzML");
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
						case "fileDescription":
							// Schema requirements: one instance of this element
							//ReadFileDescription(_reader.ReadSubtree());
							//_reader.ReadEndElement(); // "fileDescription" must have child nodes
							reader.Skip();
							break;
						case "referenceableParamGroupList":
							// Schema requirements: zero to one instances of this element
							//ReadReferenceableParamGroupList(_reader.ReadSubtree());
							//_reader.ReadEndElement(); // "referenceableParamGroupList" must have child nodes
							reader.Skip();
							break;
						case "sampleList":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "softwareList":
							// Schema requirements: one instance of this element
							reader.Skip();
							break;
						case "scanSettingsList":
							// Schema requirements: zero to one instances of this element
							reader.Skip();
							break;
						case "instrumentConfigurationList":
							// Schema requirements: one instance of this element
							reader.Skip();
							break;
						case "dataProcessingList":
							// Schema requirements: one instance of this element
							reader.Skip();
							break;
						case "run":
							// Schema requirements: one instance of this element
							// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
							ReadRunData(reader.ReadSubtree(), data, ref dataIndex);
							// "run" might not have any child nodes
							// We will either consume the EndElement, or the same element that was passed to ReadRunData (in case of no child nodes)
							reader.Read();
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
		/// Handle the child nodes of the run element
		/// Called by ReadMzML (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single "run" element</param>
		/// <param name="data">IdentData list</param>
		/// <param name="dataIndex">Indexed value</param>
		private static void ReadRunData(XmlReader reader, List<IdentData> data, ref int dataIndex)
		{
			reader.MoveToContent();
			reader.ReadStartElement("run"); // Throws exception if we are not at the "run" tag.
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
					case "referenceableParamGroupRef":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "cvParam":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "userParam":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "spectrumList":
						// Schema requirements: zero to one instances of this element
						// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
						if (dataIndex < data.Count)
						{
							ReadSpectrumList(reader.ReadSubtree(), data, ref dataIndex);
						}
						// "spectrumList" might not have any child nodes
						// We will either consume the EndElement, or the same element that was passed to ReadSpectrumList (in case of no child nodes)
						reader.Read();
						break;
					case "chromatogramList":
						// Schema requirements: zero to one instances of this element
						reader.Skip();
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
		}

		/// <summary>
		/// Handle the child nodes of a spectrumList element
		/// Called by ReadRunData (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single spectrumList element</param>
		/// <param name="data">IdentData list</param>
		/// <param name="dataIndex">Indexed value</param>
		private static void ReadSpectrumList(XmlReader reader, List<IdentData> data, ref int dataIndex)
		{
			reader.MoveToContent();
			reader.ReadStartElement("spectrumList"); // Throws exception if we are not at the "SpectrumIdentificationList" tag.
			while (reader.ReadState == ReadState.Interactive)
			{
				// Handle exiting out properly at EndElement tags
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}
				if (reader.Name == "spectrum")
				{
					var idValue = reader.GetAttribute(data[dataIndex].IdField);

					if (data[dataIndex].IdValue == idValue)
					{
						// Schema requirements: zero to many instances of this element
						// Use reader.ReadSubtree() to provide an XmlReader that is only valid for the element and child nodes
						var fixedValue = ReadSpectrum(reader.ReadSubtree());
						// "spectrum" might not have any child nodes
						// We will either consume the EndElement, or the same element that was passed to ReadSpectrum (in case of no child nodes)
						reader.Read();
						while (data[dataIndex].IdValue == idValue)
						{
							data[dataIndex].ExperMzFixed = fixedValue;
							dataIndex++;
							if (dataIndex >= data.Count)
							{
								break;
							}
						}
					}
					else
					{
						reader.Skip();
					}
				}
				else
				{
					reader.Skip();
				}
				if (dataIndex >= data.Count)
				{
					break;
				}
			}
			reader.Close();
		}

		/// <summary>
		/// Handle a single spectrum element and child nodes
		/// Called by ReadSpectrumList (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single spectrum element</param>
		private static double ReadSpectrum(XmlReader reader)
		{
			reader.MoveToContent();
			//string index = reader.GetAttribute("index");
			//Console.WriteLine("Reading spectrum indexed by " + index);
			// This is correct for Thermo files converted by msConvert, but need to implement for others as well
			string spectrumId = reader.GetAttribute("id"); // Native ID in mzML_1.1.0
			//int scanNum = Convert.ToInt32(spectrumId.Substring(spectrumId.LastIndexOf("scan=") + 5));
			// TODO: Get rid of this hack, use something with nativeID. May involve special checks for mzML version
			//int scanNum = _artificialScanNum++;
			//int defaultArraySize = Convert.ToInt32(reader.GetAttribute("defaultArrayLength"));
			reader.ReadStartElement("spectrum"); // Throws exception if we are not at the "spectrum" tag.
			//bool is_ms_ms = false;
			//int msLevel = 0;
			//bool centroided = false;
			List<Precursor> precursors = new List<Precursor>();
			List<ScanData> scans = new List<ScanData>();
			//List<BinaryDataArray> bdas = new List<BinaryDataArray>();
			while (reader.ReadState == ReadState.Interactive)
			{
				// Handle exiting out properly at EndElement tags
				if (reader.NodeType != XmlNodeType.Element)
				{
					reader.Read();
					continue;
				}
				//////////////////////////////////////////////////////////////////////////////////////
				/// 
				/// MS1 Spectra: only need Spectrum data: scanNum, MSLevel, ElutionTime, mzArray, IntensityArray
				/// 
				/// MS2 Spectra: use ProductSpectrum; adds ActivationMethod and IsolationWindow
				/// 
				//////////////////////////////////////////////////////////////////////////////////////
				switch (reader.Name)
				{
					case "referenceableParamGroupRef":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "cvParam":
						// Schema requirements: zero to many instances of this element
						/* MAY supply a *child* term of MS:1000465 (scan polarity) only once
						 *   e.g.: MS:1000129 (negative scan)
						 *   e.g.: MS:1000130 (positive scan)
						 * MUST supply a *child* term of MS:1000559 (spectrum type) only once
						 *   e.g.: MS:1000322 (charge inversion mass spectrum)
						 *   e.g.: MS:1000325 (constant neutral gain spectrum)
						 *   e.g.: MS:1000326 (constant neutral loss spectrum)
						 *   e.g.: MS:1000328 (e/2 mass spectrum)
						 *   e.g.: MS:1000341 (precursor ion spectrum)
						 *   e.g.: MS:1000579 (MS1 spectrum)
						 *   e.g.: MS:1000580 (MSn spectrum)
						 *   e.g.: MS:1000581 (CRM spectrum)
						 *   e.g.: MS:1000582 (SIM spectrum)
						 *   e.g.: MS:1000583 (SRM spectrum)
						 *   e.g.: MS:1000620 (PDA spectrum)
						 *   e.g.: MS:1000627 (selected ion current chromatogram)
						 *   e.g.: MS:1000789 (enhanced multiply charged spectrum)
						 *   e.g.: MS:1000790 (time-delayed fragmentation spectrum)
						 *   et al.
						 * MUST supply term MS:1000525 (spectrum representation) or any of its children only once
						 *   e.g.: MS:1000127 (centroid spectrum)
						 *   e.g.: MS:1000128 (profile spectrum)
						 * MAY supply a *child* term of MS:1000499 (spectrum attribute) one or more times
						 *   e.g.: MS:1000285 (total ion current)
						 *   e.g.: MS:1000497 (zoom scan)
						 *   e.g.: MS:1000504 (base peak m/z)
						 *   e.g.: MS:1000505 (base peak intensity)
						 *   e.g.: MS:1000511 (ms level)
						 *   e.g.: MS:1000527 (highest observed m/z)
						 *   e.g.: MS:1000528 (lowest observed m/z)
						 *   e.g.: MS:1000618 (highest observed wavelength)
						 *   e.g.: MS:1000619 (lowest observed wavelength)
						 *   e.g.: MS:1000796 (spectrum title)
						 *   et al.
						 */
						//switch (reader.GetAttribute("accession"))
						//{
						//	case "MS:1000127":
						//		// name="centroid spectrum"
						//		centroided = true;
						//		break;
						//	case "MS:1000128":
						//		// name="profile spectrum"
						//		is_ms_ms = false;
						//		break;
						//	case "MS:1000511":
						//		// name="ms level"
						//		msLevel = Convert.ToInt32(reader.GetAttribute("value"));
						//		break;
						//	case "MS:1000579":
						//		// name="MS1 spectrum"
						//		is_ms_ms = false;
						//		break;
						//	case "MS:1000580":
						//		// name="MSn spectrum"
						//		is_ms_ms = true;
						//		break;
						//}
						reader.Read(); // Consume the cvParam element (no child nodes)
						break;
					case "userParam":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "scanList":
						// Schema requirements: zero to one instances of this element
						scans.AddRange(ReadScanList(reader.ReadSubtree()));
						reader.ReadEndElement(); // "scanList" must have child nodes
						break;
					case "precursorList":
						// Schema requirements: zero to one instances of this element
						precursors.AddRange(ReadPrecursorList(reader.ReadSubtree()));
						reader.ReadEndElement(); // "precursorList" must have child nodes
						break;
					case "productList":
						// Schema requirements: zero to one instances of this element
						reader.Skip();
						break;
					case "binaryDataArrayList":
						// Schema requirements: zero to one instances of this element
						//bdas.AddRange(ReadBinaryDataArrayList(reader.ReadSubtree(), defaultArraySize));
						//reader.ReadEndElement(); // "binaryDataArrayList" must have child nodes
						reader.Skip();
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
			// Assume the lists contain only a single element
			double precursorMass = scans[0].MonoisotopicMz;
			if (precursorMass == 0.0)
			{
				if (precursors.Count > 0 && precursors[0].Ions.Count > 0)
				{
					precursorMass = precursors[0].Ions[0].SelectedIonMz;
				}
			}
			return precursorMass;
		}

		/// <summary>
		/// Handle a single scanList element and child nodes
		/// Called by ReadSpectrum (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single scanList element</param>
		/// <returns></returns>
		private static List<ScanData> ReadScanList(XmlReader reader)
		{
			reader.MoveToContent();
			int count = Convert.ToInt32(reader.GetAttribute("count"));
			List<ScanData> scans = new List<ScanData>();
			reader.ReadStartElement("scanList"); // Throws exception if we are not at the "scanList" tag.
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
					case "referenceableParamGroupRef":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "cvParam":
						// Schema requirements: zero to many instances of this element
						/* MUST supply a *child* term of MS:1000570 (spectra combination) only once
						 *   e.g.: MS:1000571 (sum of spectra)
						 *   e.g.: MS:1000573 (median of spectra)
						 *   e.g.: MS:1000575 (mean of spectra)
						 *   e.g.: MS:1000795 (no combination)
						 */
						switch (reader.GetAttribute("accession"))
						{
							case "MS:1000571":
								// name="sum of spectra"
								break;
							case "MS:1000573":
								// name="median of spectra"
								break;
							case "MS:1000575":
								// name="mean of spectra"
								break;
							case "MS:1000795":
								// name="no combination"
								break;
						}
						reader.Read(); // Consume the cvParam element (no child nodes)
						break;
					case "userParam":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "scan":
						// Schema requirements: one to many instances of this element
						scans.Add(ReadScan(reader.ReadSubtree()));
						reader.Read(); // "scan" might not have child nodes
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
			return scans;
		}

		/// <summary>
		/// Handle a single scan element and child nodes
		/// Called by ReadSpectrum (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single scan element</param>
		/// <returns></returns>
		private static ScanData ReadScan(XmlReader reader)
		{
			reader.MoveToContent();
			reader.ReadStartElement("scan"); // Throws exception if we are not at the "scan" tag.
			ScanData scan = new ScanData();
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
					case "referenceableParamGroupRef":
						// Schema requirements: zero to many instances of this element
						reader.Skip();
						break;
					case "cvParam":
						// Schema requirements: zero to many instances of this element
						/* MAY supply a *child* term of MS:1000503 (scan attribute) one or more times
						 *   e.g.: MS:1000011 (mass resolution)
						 *   e.g.: MS:1000015 (scan rate)
						 *   e.g.: MS:1000016 (scan start time)
						 *   e.g.: MS:1000502 (dwell time)
						 *   e.g.: MS:1000512 (filter string)
						 *   e.g.: MS:1000616 (preset scan configuration)
						 *   e.g.: MS:1000800 (mass resolving power)
						 *   e.g.: MS:1000803 (analyzer scan offset)
						 *   e.g.: MS:1000826 (elution time)
						 *   e.g.: MS:1000880 (interchannel delay)
						 * MAY supply a *child* term of MS:1000018 (scan direction) only once
						 *   e.g.: MS:1000092 (decreasing m/z scan)
						 *   e.g.: MS:1000093 (increasing m/z scan)
						 * MAY supply a *child* term of MS:1000019 (scan law) only once
						 *   e.g.: MS:1000094 (exponential)
						 *   e.g.: MS:1000095 (linear)
						 *   e.g.: MS:1000096 (quadratic)
						 */
						switch (reader.GetAttribute("accession"))
						{
							case "MS:1000016":
								// name="scan start time"
								scan.StartTime = Convert.ToDouble(reader.GetAttribute("value"));
								break;
							case "MS:1000512":
								// name="filter string"
								break;
							case "MS:1000616":
								// name="preset scan configuration"
								break;
							case "MS:1000927":
								// name="ion injection time"
								break;
							case "MS:1000826":
								// name="elution time"
								//startTime = Convert.ToDouble(reader.GetAttribute("value"));
								break;
						}
						reader.Read(); // Consume the cvParam element (no child nodes)
						break;
					case "userParam":
						// Schema requirements: zero to many instances of this element
						if (reader.GetAttribute("name") == "[Thermo Trailer Extra]Monoisotopic M/Z:")
						{
							scan.MonoisotopicMz = Convert.ToDouble(reader.GetAttribute("value"));
						}
						reader.Read(); // Consume the userParam element (no child nodes)
						break;
					case "scanWindowList":
						// Schema requirements: zero to one instances of this element
						//ReadScanList(reader.ReadSubtree());
						//reader.ReadEndElement(); // "scanWindowList" must have child nodes
						reader.Skip();
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
			return scan;
		}

		/// <summary>
		/// Handle a single precursorList element and child nodes
		/// Called by ReadSpectrum (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single precursorList element</param>
		/// <returns></returns>
		private static List<Precursor> ReadPrecursorList(XmlReader reader)
		{
			reader.MoveToContent();
			int count = Convert.ToInt32(reader.GetAttribute("count"));
			List<Precursor> precursors = new List<Precursor>();
			reader.ReadStartElement("precursorList"); // Throws exception if we are not at the "precursorList" tag.
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
					case "precursor":
						// Schema requirements: one to many instances of this element
						precursors.Add(ReadPrecursor(reader.ReadSubtree()));
						reader.ReadEndElement(); // "SpectrumIdentificationItem" must have child nodes
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
			return precursors;
		}

		/// <summary>
		/// Handle a single precursor element and child nodes
		/// Called by ReadPrecursorList (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single precursor element</param>
		/// <returns></returns>
		private static Precursor ReadPrecursor(XmlReader reader)
		{
			reader.MoveToContent();
			reader.ReadStartElement("precursor"); // Throws exception if we are not at the "precursor" tag.
			XmlReader innerReader;
			Precursor precursor = new Precursor();
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
					case "isolationWindow":
						// Schema requirements: zero to one instances of this element
						innerReader = reader.ReadSubtree();
						innerReader.MoveToContent();
						innerReader.ReadStartElement("isolationWindow"); // Throws exception if we are not at the "selectedIon" tag.
						while (innerReader.ReadState == ReadState.Interactive)
						{
							// Handle exiting out properly at EndElement tags
							if (innerReader.NodeType != XmlNodeType.Element)
							{
								innerReader.Read();
								continue;
							}
							switch (innerReader.Name)
							{
								case "referenceableParamGroupRef":
									// Schema requirements: zero to many instances of this element
									innerReader.Skip();
									break;
								case "cvParam":
									// Schema requirements: zero to many instances of this element
									/* MUST supply a *child* term of MS:1000792 (isolation window attribute) one or more times
									 *   e.g.: MS:1000827 (isolation window target m/z)
									 *   e.g.: MS:1000828 (isolation window lower offset)
									 *   e.g.: MS:1000829 (isolation window upper offset)
									 */
									switch (innerReader.GetAttribute("accession"))
									{
										case "MS:1000827":
											// name="isolation window target m/z"
											precursor.IsolationWindowTargetMz = Convert.ToDouble(innerReader.GetAttribute("value"));
											break;
										case "MS:1000828":
											// name="isolation window lower offset"
											precursor.IsolationWindowLowerOffset = Convert.ToDouble(innerReader.GetAttribute("value"));
											break;
										case "MS:1000829":
											// name="isolation window upper offset"
											precursor.IsolationWindowUpperOffset = Convert.ToDouble(innerReader.GetAttribute("value"));
											break;
									}
									innerReader.Read(); // Consume the cvParam element (no child nodes)
									break;
								case "userParam":
									// Schema requirements: zero to many instances of this element
									innerReader.Skip();
									break;
								default:
									innerReader.Skip();
									break;
							}
						}
						innerReader.Close();
						reader.Read(); // "selectedIon" might not have child nodes
						// We will either consume the EndElement, or the same element that was passed to ReadSpectrum (in case of no child nodes)
						break;
					case "selectedIonList":
						// Schema requirements: zero to one instances of this element
						// mzML_1.0.0: one instance of this element
						precursor.Ions = ReadSelectedIonList(reader.ReadSubtree());
						reader.ReadEndElement(); // "selectedIonList" must have child nodes
						break;
					case "activation":
						// Schema requirements: one instance of this element
						reader.Skip();
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
			return precursor;
		}

		/// <summary>
		/// Handle a single selectedIonList element and child nodes
		/// Called by ReadPrecursor (xml hierarchy)
		/// </summary>
		/// <param name="reader">XmlReader that is only valid for the scope of the single selectedIonList element</param>
		/// <returns></returns>
		private static List<SelectedIon> ReadSelectedIonList(XmlReader reader)
		{
			reader.MoveToContent();
			//int count = Convert.ToInt32(reader.GetAttribute("count"));
			var ions = new List<SelectedIon>();
			reader.ReadStartElement("selectedIonList"); // Throws exception if we are not at the "selectedIonList" tag.
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
					case "selectedIon":
						// Schema requirements: one to many instances of this element
						var ion = new SelectedIon();
						var innerReader = reader.ReadSubtree();
						innerReader.MoveToContent();
						innerReader.ReadStartElement("selectedIon"); // Throws exception if we are not at the "selectedIon" tag.
						while (innerReader.ReadState == ReadState.Interactive)
						{
							// Handle exiting out properly at EndElement tags
							if (innerReader.NodeType != XmlNodeType.Element)
							{
								innerReader.Read();
								continue;
							}
							switch (innerReader.Name)
							{
								case "referenceableParamGroupRef":
									// Schema requirements: zero to many instances of this element
									innerReader.Skip();
									break;
								case "cvParam":
									// Schema requirements: zero to many instances of this element
									/* MUST supply a *child* term of MS:1000455 (ion selection attribute) one or more times
									 *   e.g.: MS:1000041 (charge state)
									 *   e.g.: MS:1000042 (intensity)
									 *   e.g.: MS:1000633 (possible charge state)
									 *   e.g.: MS:1000744 (selected ion m/z)
									 */
									switch (innerReader.GetAttribute("accession"))
									{
										case "MS:1000041":
											// name="charge state"
											ion.Charge = (int)Convert.ToDouble(innerReader.GetAttribute("value"));
											break;
										case "MS:1000744":
											// name="selected ion m/z"
											ion.SelectedIonMz = Convert.ToDouble(innerReader.GetAttribute("value"));
											break;
									}
									innerReader.Read(); // Consume the cvParam element (no child nodes)
									break;
								case "userParam":
									// Schema requirements: zero to many instances of this element
									innerReader.Skip();
									break;
								default:
									innerReader.Skip();
									break;
							}
						}
						innerReader.Close();
						ions.Add(ion);
						reader.Read(); // "selectedIon" might not have child nodes
						// We will either consume the EndElement, or the same element that was passed to ReadSpectrum (in case of no child nodes)
						break;
					default:
						reader.Skip();
						break;
				}
			}
			reader.Close();
			return ions;
		}
	}
}
