using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCarmada.Server
{
    class NameList
    {
        private string filePath;
        public List<string> Names = new List<string>();

        public NameList(string fileName)
        {
            filePath = Path.GetFullPath(Path.Combine("Settings/", fileName));

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    Names.Add(line);
                }
            }
            else
            {
                File.Create(filePath);
            }
        }

        public bool Contains(string name)
        {
            foreach (var n in Names)
            {
                if (n.Equals(name))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddName(String name)
        {
            Names.Add(name);
            Write();
        }

        public void RemoveName(string name)
        {
            Names.Remove(name);
            Write();
        }

        public void Write()
        {
            File.WriteAllLines(filePath, Names);
        }
    }
}
