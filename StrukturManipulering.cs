using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using VMS.TPS.Common.Model.API;
using System.Windows.Media;

namespace StrukturMall
{
    internal class StrukturManipulering
    {
        private string folderPath = @"\\ltvastmanland.se\ltv\shares\rhosonk\Strålbehandling\Scripting\StrukturMall\Strukturmallar\";

        public StrukturManipulering(Patient pat)
        {
            pat.BeginModifications();
        }

        public List<string> GetAllTemplateNames()
        {
            string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");

            List<string> templates = new List<string>();

            foreach (string xmlFile in xmlFiles)
            {
                string tempTemplate = Path.GetFileName(xmlFile).Replace(".xml", "");
                templates.Add(tempTemplate);
            }
            return templates;
        }

        public StructureTemplates ImportStructureTemplate(string fileName)
        {
            StructureTemplates curr_structureTemplate;
            // Get all XML files in the folder
            string xmlFilePath = folderPath + fileName;

            XmlRootAttribute xRoot = new XmlRootAttribute
            {
                ElementName = "StructureTemplate",
                Namespace = ""  // Matches the empty namespace in your XML
            };

            XmlSerializer serializer = new XmlSerializer(typeof(StructureTemplates), xRoot);

            // Deserialize the XML file into a StructureTemplates object
            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                curr_structureTemplate = (StructureTemplates)serializer.Deserialize(reader);
            }
            return curr_structureTemplate;
        }

        public StructureSet FindARTPlanStructureSet(Patient pat, StructureSet strSet)
        {
            StructureSet dtDos = strSet.Copy();
            string dtID = "DTdos" + dtDos.Image.CreationDateTime.Value.ToString("yyMMdd");

            dtID = FindSuitableID(dtID, pat);
            dtDos.Id = dtID;
            dtDos.Image.Id = dtID;
            MergeRectumStructures(dtDos); // skapar union av rectum och anorectum
            MergeLarynxStructures(dtDos); // skapar union av Glottis och LarynxSG
            return dtDos;
        }

        public string FindSuitableID(string dtID, Patient pat)
        {
            bool checkId = pat.StructureSets.Any(ss => ss.Id.ToLower().Equals(dtID.ToLower()));
            if (checkId)
            {
                int i = 1;
                string temp_id = "";
                while (checkId)
                {
                    temp_id = dtID + "_" + i;
                    i++;
                    checkId = pat.StructureSets.Any(ss => ss.Id.ToLower().Equals(temp_id.ToLower()));
                }
                dtID = temp_id;
            }
            return dtID;
        }

        // skapar union av rectum och anorectum
        public void MergeRectumStructures(StructureSet strSet)
        {
            Structure temp_Rectum = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Rectum"));
            Structure temp_Anorectum = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Anorectum"));
            if (temp_Rectum != null && temp_Anorectum != null)
            {
                if (temp_Anorectum.CanConvertToHighResolution())
                    temp_Anorectum.ConvertToHighResolution();
                if (temp_Rectum.CanConvertToHighResolution())
                    temp_Rectum.ConvertToHighResolution();
                strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Rectum")).SegmentVolume = temp_Rectum.SegmentVolume.Or(temp_Anorectum.SegmentVolume);
            }
        }
        // skapar union av Glottis och LarynxSG
        public void MergeLarynxStructures(StructureSet strSet)
        {
            Structure temp_Glottis = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Glottis"));
            Structure temp_LarynxSG = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("LarynxSG"));
            Structure temp_Larynx = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Larynx"));

            if (temp_Glottis != null && temp_LarynxSG != null && temp_Larynx == null)
            {
                if (temp_Glottis.CanConvertToHighResolution())
                    temp_Glottis.ConvertToHighResolution();
                if (temp_LarynxSG.CanConvertToHighResolution())
                    temp_LarynxSG.ConvertToHighResolution();
                Structure larynx = strSet.AddStructure("Organ", "Larynx");
                if (larynx.CanConvertToHighResolution())
                    larynx.ConvertToHighResolution();
                larynx.SegmentVolume = temp_Glottis.SegmentVolume.Or(temp_LarynxSG.SegmentVolume);
            }
        }

        public void CopyStructuresToDTDos(StructureSet dtDos, StructureTemplates curr_structureTemplate)
        {
            // Hittar alla strukturer i ART-plan som inte finns i valt template - lägger i lista
            List<Structure> listOfStructuresToRemove = new List<Structure>();
            foreach (Structure temp_structure in dtDos.Structures)
            {
                var removeStructure = curr_structureTemplate.Structures.FirstOrDefault(s => s.ID.Equals(temp_structure.Id));
                if (removeStructure == null && temp_structure.DicomType != "")
                {
                    listOfStructuresToRemove.Add(temp_structure);
                }
            }
            // Tar bort alla strukturer som finns i listan
            foreach (Structure temp_structure in listOfStructuresToRemove)
            {
                Console.WriteLine("Tar bort: " + temp_structure.Id);
                bool ok = temp_structure.CanEditSegmentVolume(out string errorMsg);
                if (ok)
                    dtDos.RemoveStructure(temp_structure);
                else
                {
                    Console.WriteLine(errorMsg);
                }
            }

            // Lägg till tomma strukturer från template till strukturset (dvs PTV, CTV, PRV_xxx)
            List<StructureTemplateStructuresStructure> listOfStructuresToAdd = new List<StructureTemplateStructuresStructure>();

            foreach (StructureTemplateStructuresStructure templateStruktur in curr_structureTemplate.Structures)
            {
                var addStructure = dtDos.Structures.FirstOrDefault(s => s.Id.Equals(templateStruktur.ID));
                if (addStructure == null)
                {
                    listOfStructuresToAdd.Add(templateStruktur);
                }
            }
            foreach (StructureTemplateStructuresStructure templateStruktur in listOfStructuresToAdd)
            {
                Console.WriteLine("Lägger till: " + templateStruktur.ID);
                bool structOk = dtDos.CanAddStructure(templateStruktur.Identification.First().VolumeType, templateStruktur.ID);
                if (structOk)
                {
                    dtDos.AddStructure(templateStruktur.Identification.First().VolumeType, templateStruktur.ID);
                }
                else
                {
                    dtDos.AddStructure("Control", templateStruktur.ID);
                }
            }
        }

        public void ColorCorrector(StructureSet dtDos, StructureTemplates curr_structureTemplate)
        {
            // Fixar färgerna till samma färger som i aktuell template
            foreach (Structure s in dtDos.Structures)
            {
                StructureTemplateStructuresStructure templateStruktur = curr_structureTemplate.Structures.FirstOrDefault(sss => sss.ID.Equals(s.Id));
                if (templateStruktur != null)
                {
                    //get color from template
                    string input = templateStruktur.ColorAndStyle;

                    // Split by space
                    string[] parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    try
                    {
                        // Parse the last three parts as integers
                        byte r = byte.Parse(parts[1]);
                        byte g = byte.Parse(parts[2]);
                        byte b = byte.Parse(parts[3]);
                        //alfa-värde (opacity)
                        byte a = AlphaValue(s);

                        Color newcol = Color.FromArgb(a, r, g, b);
                        s.Color = newcol;
                    }
                        catch (Exception e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            }
        }
        private byte AlphaValue(Structure s)
        {
            byte alfa = 128;
            if (s.DicomType.Equals("PTV") || s.DicomType.Equals("CTV"))
            {
                alfa = 200;
            }
            //else if ()
            //{
            //    alfa = 200;
            //}
            return alfa;
        }
    }
}

