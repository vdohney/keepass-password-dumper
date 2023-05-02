# KeePass 2.X Master Password Dumper

KeePass Master Password Dumper is a simple proof-of-concept tool used to dump the master password from KeePass's memory. Apart from the first password character, it is mostly able to recover the password in plaintext. No code execution on the target system is required, just a memory dump. It doesn't matter where the memory comes from - can be the **process dump, swap file (`pagefile.sys`), hibernation file (`hiberfil.sys`) or RAM dump** of the entire system. **It doesn't matter whether or not the workspace is locked**. It is also possible to dump the password from RAM after KeePass is no longer running, although the chance of that working goes down with the time it's been since then.

Tested with `KeePass 2.51.1` on Windows. Should work for Linux and macOS versions as well. Finding was confirmed by Dominik Reichl, KeePass's author, [here](https://sourceforge.net/p/keepass/discussion/329220/thread/f3438e6283/). I appreciate Dominik's fast response. Hopefully it will be fixed soon!

## Setup
1. [Install .NET](https://dotnet.microsoft.com/en-us/download) (most major operating systems supported).
2. `dotnet run PATH_TO_DUMP`

The easiest way to test this on Windows is to create a process dump in the task manager by right-clicking the KeePass process and selecting "Create dump file".

![Usage example](assets/anim.gif)

## Should You Be Worried?

Depends on your threat model. **If your computer is already infected by malware that's running in the background with the privileges of your user, this finding doesn't make your situation much worse.** However, it might be easier for the malware to be stealthy and evade the antivirus, since unlike KeeTheft or KeeFarce, no process injection or other type of code execution is necessary. 

If you have a reasonable suspicion that someone could obtain access to your computer and conduct forensic analysis, this could be bad. Worst case scenario is that the master password will be recovered, despite KeePass being locked or not running at all. 

If you use full disk encryption with a strong password and your system is clean, you should be fine. No one can steal your passwords remotely over the internet with this finding alone. 

## How It Works

KeePass 2.X uses a custom-developed text box for password entry, `SecureTextBoxEx`. This text box is used for the master password entry, but also in other places in KeePass (so the attack can also be used to recover contents of them).

The flaw exploited here is that for every character typed, a leftover string is created in memory. Because of how .NET works, it is nearly impossible to get rid of it once it gets created. For example, when "Password" is typed, it will result in these leftover strings: •a, ••s, •••s, ••••w, •••••o, ••••••r, •••••••d. The POC application searches the dump for these patterns and offers a likely password character for each position in the password. 

Reliability of this attack can be influenced depending on how the password was typed and how many passwords were typed per session. However, I've discovered that even if there are multiple passwords per session or typos, the way .NET CLR allocates these strings means that they are likely to be nicely ordered in memory. So if three different passwords were typed, you are likely to get three candidates for each character position in that order, which makes it easy possible to recover all three passwords. 

## Dev

It's a quick POC, so likely not very reliable and robust. Please create a pull request if you happen to find an issue and fix it. It would also be cool to port this to a language like Python. 

Allowed password characters are currently hardcoded like this: `^[\x20-\x7E]+$` (all printable ASCII characters and space)