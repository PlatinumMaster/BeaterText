using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeaterText
{
    class TextLexer
    {
        private StreamReader input;
        private BinaryWriter b;
        public TextLexer(string text, string output)
        {
            // Initialize the text file we will read from.
            input = new StreamReader(text);
            b = new BinaryWriter(File.Open(output, FileMode.Open));
            WriteText();
            b.Close();
        }

        public void WriteText()
        {
            List<string> strings = new List<string>();
            bool parsing = true;

            while (parsing)
            {
                string l = input.ReadLine();
                parsing = !l.Equals("END_MSG");

                if (l.StartsWith("["))
                {
                    string str = "";
                    while (!l.EndsWith("]"))
                    {
                        str += l.Substring(l.IndexOf("\"") + 1, l.LastIndexOf("\"") - l.IndexOf("\"") - 1);
                        l = input.ReadLine();
                    }
                    str += l.Substring(l.IndexOf("\"") + 1, l.LastIndexOf("\"") - l.IndexOf("\"") - 1);
                    strings.Add(str);
                    //Console.WriteLine(str);
                }
                else if (l.StartsWith("#"))
                    continue;
            }
            input.Close();

            b.Write((ushort)0x1); // nSections. We only use 1.
            b.Write((ushort)strings.Count); // Number of entries.

            uint sectionSize = 0;
            List<int> characterCounts = new List<int>();

            // Determine each string length;
            for (int i = 0; i < strings.Count; i++)
            {
                int count = 0;
                for (int j = 0; j < strings[i].Count(); j++)
                {
                    switch(strings[i][j].ToString())
                    {
                        case "\\":
                            if (strings[i][j+1].Equals('x'))
                                j += 6;
                            else
                                j += 2;
                            break;
                        default:
                            j++;
                            break;
                    }
                    count++;
                }
                characterCounts.Add(count);
                sectionSize += Convert.ToUInt32(count * 2);
            }

            b.Write(sectionSize); // Section size.
            b.Write(0); // Unknown.
            b.Write(0x10); // Section offset.

            // Begin writing the section.
            b.Write(sectionSize); // Section size.

            int offset = 4 + (8 * strings.Count);
            for (int i = 0; i < strings.Count; i++)
            {
                b.Write((uint)offset); // Offset.
                b.Write((ushort)characterCounts[i]);
                b.Write((ushort)0x0);
                offset += characterCounts[i] * 0x2;
            }

            int mainKey = 0x7C89;
            for (int i = 0; i < strings.Count; i++)
            {
                int key = mainKey;
                for (int j = 0; j < strings[i].Length; j++)
                {
                    if (j == strings[i].Length - 1)
                    {
                        b.Write((ushort)key ^ 0xFFFF);
                        break;
                    }
                    switch (strings[i][j].ToString())
                    {
                        case "\\":
                            if (strings[i][j + 1].Equals('x'))
                            {
                                b.Write(EncryptCharacter(strings[i].Substring(j, 6), key));
                                j += 5;
                            }
                            else
                            {
                                b.Write(EncryptCharacter(strings[i].Substring(j, 2), key));
                                j += 1;
                            }
                            break;
                        default:
                            b.Write(EncryptCharacter(strings[i][j].ToString(), key));
                            continue;
                    }
                    key = (key << 3 | key >> 13) & 0xFFFF;
                }
                mainKey += 0x2983;
                mainKey = mainKey > 0xFFFF ? mainKey - 0x10000 : mainKey;
            }

            // We're done... I hope.
            b.Close();

        }
        public ushort EncryptCharacter(string decrypted, int key)
        {
            switch (decrypted)
            {
                case "$":
                    return Convert.ToUInt16(0xFFFF ^ key);
                case "\\n":
                    return Convert.ToUInt16(0xFFFE ^ key);
                default:
                    if (decrypted.StartsWith('\\') && decrypted[1] == 'x')
                        return Convert.ToUInt16(ushort.Parse(decrypted.Substring(2), 
                            System.Globalization.NumberStyles.HexNumber) ^ key);
                    return Convert.ToUInt16(char.Parse(decrypted) ^ key);
            }
        }
    }
}
