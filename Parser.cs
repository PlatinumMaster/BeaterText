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

                    sw.WriteLine($"# STR_{i}");
                    sw.Write("[\"");

                    for (int j = characterCount[i] - 1; j >= 0; j--)
                    {
                        s.Insert(0, DecryptCharacter(encrypted_text[j], key));
                        if (DecryptCharacter(encrypted_text[j], key).Equals("\\n"))
                            s.Insert(2, "\",\n\"");
                        key = (key >> 3 | key << 13) & 0xFFFF;
                    }


                    s.Replace("\\xF000븁\\x0000", "\\c"); // Clear character for Gen V.
                    s.Replace("\\xF000븀\\x0000", "\\l"); // Scroll to next line.
                    s.Replace("\\xF000Ā\\x0001\\x0000", "{PLAYER}"); // Player name.
                    s.Replace("\\xF000Ā\\x0001\\x0001", "{RIVAL}"); // Rival name.

                    sw.Write(s.ToString());
                    sw.Write("\"]\n\n");
                }

                sw.WriteLine("END_MSG");
            }

        }
        public string DecryptCharacter(ushort encrypted, int key)
        {
            switch (encrypted ^ key)
            {
                case 0xFFFF:
                    return "$";
                case 0xFFFE:
                    return "\\n";
                default:
                    if ((encrypted ^ key) > 0x14 && (encrypted ^ key) < 0xF000)
                        return Convert.ToChar(encrypted ^ key).ToString();
                    else
                        return $"\\x{(encrypted ^ key):X4}";
            }
        }
    }
}
