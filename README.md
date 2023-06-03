# KeePass 2.X Master Password Dumper ([CVE-2023-32784](https://nvd.nist.gov/vuln/detail/CVE-2023-32784))

## Update

The vulnerability was assigned [CVE-2023-32784](https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2023-32784) and fixed in [KeePass 2.54](https://keepass.info/news/n230603_2.54.html). Thanks again to Dominik Reichl for his fast response and creative fix!

Clarification: **the password has to be typed on a keyboard, not copied from a clipboard** (see the How it works sections).

### What can you do
First, **update to KeePass 2.54 or higher**.

Second, if you've been using KeePass for a long time, your master password (and potentially other passwords) could be in your pagefile/swapfile, hibernation file and crash dump(s). Depending on your paranoia level, you can consider these steps to resolve the issue:

1. Change your master password
0. Delete crash dumps (depends on your OS, on Windows at least `C:\Windows\memory.dmp`, but maybe there are others)
0. Delete hibernation file
0. Delete pagefile/swapfile (can be quite annoying, don't forget to enable it back again)
0. Overwrite deleted data on the HDD to prevent carving (e.g. [Cipher](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/cipher) with `/w` on Windows)
0. Restart your computer

Or just overwrite your HDD and do a fresh install of your OS.

Incomplete list of products **that are not impacted** (please create a pull request or an issue for adding more). Rule of thumb is that if it isn't the original KeePass 2.X app written in .NET, it's likely not affected.

- [KeePassXC](https://github.com/keepassxreboot/keepassxc/discussions/9433)
- [Strongbox](https://www.reddit.com/r/strongbox/comments/13jg2pz/keepass_2x_master_password_dumper_cve202332784/)
- [KeePass 1.X](https://sourceforge.net/p/keepass/discussion/329220/thread/f3438e6283/#08e1/2240)

----
----

KeePass Master Password Dumper is a simple proof-of-concept tool used to dump the master password from KeePass's memory. Apart from the first password character, it is mostly able to recover the password in plaintext. No code execution on the target system is required, just a memory dump. It doesn't matter where the memory comes from - can be the **process dump, swap file (`pagefile.sys`), hibernation file (`hiberfil.sys`), various crash dumps or RAM dump** of the entire system. **It doesn't matter whether or not the workspace is locked**. It is also possible to dump the password from RAM after KeePass is no longer running, although the chance of that working goes down with the time it's been since then.

Tested with `KeePass 2.53.1` on Windows (English) and `KeePass 2.47` on Debian (keepass2 package). It should work for the macOS version as well. Unfortunately, enabling the `Enter master key on secure desktop` option doesn't help in preventing the attack. PoC might have issues with databases created by older versions of KeePass, but I wasn't able to reproduce it (see [issue #4](https://github.com/vdohney/keepass-password-dumper/issues/4)).

Finding was confirmed by Dominik Reichl, KeePass's author, [here](https://sourceforge.net/p/keepass/discussion/329220/thread/f3438e6283/). I appreciate Dominik's fast response. Hopefully it will be fixed soon!

## Setup
1. [Install .NET](https://dotnet.microsoft.com/en-us/download) (most major operating systems supported).
2. Clone the repository: `git clone https://github.com/vdohney/keepass-password-dumper` or download it from GitHub
2. Enter the project directory in your terminal (Powershell on Windows) `cd keepass-password-dumper`
3. `dotnet run PATH_TO_DUMP`

The easiest way to test this on Windows is to create a process dump in the task manager by right-clicking the KeePass process and selecting "Create dump file".

![Usage example](assets/anim.gif)

4. Alternatively you can add another parameter `dotnet run PATH_TO_DUMP PATH_TO_PWDLIST` to generate a list of all possible passwords beginning from the second character. 

## Should You Be Worried?

Depends on your threat model. **If your computer is already infected by malware that's running in the background with the privileges of your user, this finding doesn't make your situation much worse.** However, it might be easier for the malware to be stealthy and evade the antivirus, since unlike KeeTheft or KeeFarce, no process injection or other type of code execution is necessary. 

If you have a reasonable suspicion that someone could obtain access to your computer and conduct forensic analysis, this could be bad. Worst case scenario is that the master password will be recovered, despite KeePass being locked or not running at all. 

If you use full disk encryption with a strong password and your system is clean, you should be fine. No one can steal your passwords remotely over the internet with this finding alone. 

## How It Works

KeePass 2.X uses a custom-developed text box for password entry, `SecureTextBoxEx`. This text box is not only used for the master password entry, but in other places in KeePass as well, like password edit boxes (so the attack can also be used to recover their contents).

The flaw exploited here is that for every character typed, a leftover string is created in memory. Because of how .NET works, it is nearly impossible to get rid of it once it gets created. For example, when "Password" is typed, it will result in these leftover strings: •a, ••s, •••s, ••••w, •••••o, ••••••r, •••••••d. The POC application searches the dump for these patterns and offers a likely password character for each position in the password. 

Reliability of this attack can be influenced depending on how the password was typed and how many passwords were typed per session. However, I've discovered that even if there are multiple passwords per session or typos, the way .NET CLR allocates these strings means that they are likely to be nicely ordered in memory. So if three different passwords were typed, you are likely to get three candidates for each character position in that order, which makes it possible to recover all three passwords. 

## Dev

It's a quick POC, so likely not very reliable and robust. Please create a pull request if you happen to find an issue and fix it.

Allowed password characters are currently hardcoded like this: `^[\x20-\x7E]+$` (all printable ASCII characters and space).

## Acknowledgements
Thanks to [adridlug](https://github.com/adridlug) for adding the possibility to auto-generate the password list, and [ynuwenhof](https://github.com/ynuwenhof) for refactoring the code.

## Related Projects
- [Python implementation of the PoC](https://github.com/CMEPW/keepass-dump-masterkey) by [CMEPW](https://github.com/CMEPW)
- [Rust implementation of the PoC](https://github.com/ynuwenhof/keedump) by [ynuwenhof](https://github.com/ynuwenhof)

I haven't checked any of them yet.
