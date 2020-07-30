using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeaterText
{
    class TextParser
    {
        private BinaryReader b;
        public TextParser(string text, string output) 
        {
            // Initialize the text file we will read from.
            b = new BinaryReader(File.Open(text, FileMode.Open));
            ParseText(text, output);
            b.Close();
        }

        public void ParseText(string text, string output)
        {
            ushort nSections = b.ReadUInt16(), nEntries = b.ReadUInt16();
            uint sectionSize = b.ReadUInt32(), unknown = b.ReadUInt32(), sectionOffset = b.ReadUInt32();

            List<uint> tableOffsets = new List<uint>();
            List<ushort> characterCount = new List<ushort>();
            List<ushort> unknown2 = new List<ushort>();

            b.BaseStream.Position += 0x4;

            using (StreamWriter sw = new StreamWriter(output))
            {
                for (int i = 0; i < nEntries; i++)
                {
                    tableOffsets.Add(b.ReadUInt32());
                    characterCount.Add(b.ReadUInt16());
                    unknown2.Add(b.ReadUInt16());
                }

                for (int i = 0; i < nEntries; i++)
                {
                    StringBuilder s = new StringBuilder();
                    b.BaseStream.Position = sectionOffset + tableOffsets[i];

                    List<ushort> encrypted_text = new List<ushort>();

                    for (int j = 0; j < characterCount[i]; j++)
                        encrypted_text.Add(b.ReadUInt16());

                    int key = encrypted_text.Last() ^ 0xFFFF;

                    for (int j = characterCount[i] - 1; j >= 0; j--)
                    {
                        encrypted_text[j] ^= (ushort)key;
                        key = (key >> 3 | key << 13) & 0xFFFF;
                    }

                    sw.WriteLine($"# STR_{i}");
                    sw.Write("[\"");
                    for (int j = 0; j < characterCount[i]; j++)
                    {
                        sw.Write(ConvertToCharacter(encrypted_text[j]));
                        if (ConvertToCharacter(encrypted_text[j]).Equals("\\n") || ConvertToCharacter(encrypted_text[j]).Equals("\\c"))
                            sw.Write("\",\n\"");
                    }
                    sw.Write("\"]\n\n");
                }
            }
        }

        public string ConvertToCharacter(ushort encrypted)
        {

            switch (encrypted)
            {
                case 0xFFFF:
                    return "$";
                case 0xFFFE:
                    return "\\n";
                default:
                    if (encrypted > 0x14 && encrypted < 0xF000)
                        return Convert.ToChar(encrypted).ToString();
                    else
                        return $"\\{encrypted:X4}";
            }
        }
    }
}
