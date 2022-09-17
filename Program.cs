using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
// inspired by http://chrisparnin.github.io/articles/2013/09/parse-git-log-output-in-c/
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
GitCommit commit = null;
var cmdArgs = Environment.GetCommandLineArgs();
Parser.Default.ParseArguments<CommandLineOptions>(cmdArgs)
    .WithParsed<CommandLineOptions>(o =>
    {
        if (o.Repo != null)
        {
            Console.WriteLine($"Verbose output enabled. Current Arguments: -v {o.Since}");
            Console.WriteLine("Quick Start Example! App is in Verbose mode!");
        }
        else
        {
            Console.WriteLine($"Current Arguments: -v {o.Since}");
            Console.WriteLine("Quick Start Example!");
        }
        string output = Utils.AllLogs(o.Since, o.Author);
        Console.WriteLine(output);

        var commits = Utils.ParseResults(output);
        Console.WriteLine(commits);


        // pull entries with #{number} and JIRA-1 project regex
        var entries = new List<String>();
        // iterate across all commmits and print out the commit message
        foreach (var c in commits)
        {
            Console.WriteLine(c.Headers["Author"]);
            Console.WriteLine(c.Headers["Date"]);
            Console.WriteLine(c.Message);
            // check for regex #{number} and JIRA-1
            foreach (Match match in Regex.Matches(c.Message, @"-\d+",
                                               RegexOptions.None,
                                               TimeSpan.FromSeconds(1)))
            {
                Console.WriteLine("Found '{0}' at position {1}", match.Value, match.Index);
                entries.Add(match.Value);
            }

            // check for regex #{number}
            foreach (Match match in Regex.Matches(c.Message, @"#\d+",
                                               RegexOptions.None,
                                               TimeSpan.FromSeconds(1)))
            {
                Console.WriteLine("Found '{0}' at position {1}", match.Value, match.Index);
                entries.Add(match.Value);
            }
        }

    });


public class CommandLineOptions
{
    [Option('s', "since", Required = false, Default = "yesterday", HelpText = "Since Time")]
    public string Since { get; set; }

    [Option('a', "author", Required = false, Default = "David Li", HelpText = "Author to search git logs for")]
    public string Author { get; set; }

    [Option('r', "repo", Required = false, HelpText = "local path to repository to parse")]
    public string Repo { get; set; }

}


public class GitCommit
{
    public GitCommit()
    {
        Headers = new Dictionary<string, string>();
        Files = new List<GitFileStatus>();
        Message = "";
    }

    public Dictionary<string, string> Headers { get; set; }
    public string Sha { get; set; }
    public string Message { get; set; }
    public List<GitFileStatus> Files { get; set; }
}

public class GitFileStatus
{
    public string Status { get; set; }
    public string File { get; set; }
}

public class Utils
{

    public static string RunProcess(string command)
    {
        // Start the child process.
        Process p = new Process();
        // Redirect the output stream of the child process.
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = "git";
        p.StartInfo.Arguments = command;
        p.Start();
        // Read the output stream first and then wait.
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return output;

    }

    static string ListShaWithFiles(string path)
    {
        var output = RunProcess(string.Format(" --git-dir={0}/.git --work-tree={1} log --name-status", path.Replace("\\", "/"), path.Replace("\\", "/")));
        return output;
    }

    public static string AllLogs(string since, string author)
    {
        var args_string = string.Format("log --all --since={0} --before=0am --author=\"{1}\"", since, author);
        Console.WriteLine(args_string);
        var output = RunProcess(args_string);
        return output;
    }

    public static List<GitCommit> ParseResults(string output)
    {
        GitCommit commit = null;
        var commits = new List<GitCommit>();
        bool processingMessage = false;
        using (var strReader = new StringReader(output))
        {
            do
            {
                var line = strReader.ReadLine();

                if (line.StartsWith("commit "))
                {
                    if (commit != null)
                        commits.Add(commit);
                    commit = new GitCommit();
                    commit.Sha = line.Split(' ')[1];
                }

                if (StartsWithHeader(line))
                {
                    var header = line.Split(':')[0];
                    var val = string.Join(":", line.Split(':').Skip(1)).Trim();

                    // headers
                    commit.Headers.Add(header, val);
                }

                if (string.IsNullOrEmpty(line) && commit.Message != null)
                {
                    Console.WriteLine("Start processing message");
                    // commit message divider
                    processingMessage = !processingMessage;
                }

                if (line.Length > 0 && processingMessage)
                {
                    // commit message.
                    commit.Message += line;
                }
                // if (line.Length > 1 && Char.IsLetter(line[0]) && line[1] == '\t')
                // {
                //     Console.WriteLine(line);
                //     var status = line.Split('\t')[0];
                //     var file = line.Split('\t')[1];
                //     commit.Files.Add(new GitFileStatus() { Status = status, File = file } );
                // }
            }
            while (strReader.Peek() != -1);
        }
        if (commit != null)
            commits.Add(commit);

        return commits;
    }

    static bool StartsWithHeader(string line)
    {
        if (line.Length > 0 && char.IsLetter(line[0]))
        {
            var seq = line.SkipWhile(ch => Char.IsLetter(ch) && ch != ':');
            return seq.FirstOrDefault() == ':';
        }
        return false;
    }


}



// void Print()
// {
//     Console.WriteLine("commit " + Sha);
//     foreach (var key in Headers.Keys)
//     {
//         Console.WriteLine(key + ":" + Headers[key]);
//     }
//     Console.WriteLine();
//     Console.WriteLine(Message);
//     Console.WriteLine();
//     foreach (var file in Files)
//     {
//         Console.WriteLine(file.Status + "\t" + file.File);
//     }
// }
// using (var strReader = new StringReader(output))
// {
//     do
//     {
//         var line = strReader.ReadLine();

//         if (line.StartsWith("commit "))
//         {
//             if (commit != null)
//                 commits.Add(commit);
//             commit = new GitCommit();
//             commit.Sha = line.Split(' ')[1];
//         }

//         if (StartsWithHeader(line))
//         {
//             var header = line.Split(':')[0];
//             var val = string.Join(":", line.Split(':').Skip(1)).Trim();

//             // headers
//             commit.Headers.Add(header, val);
//         }

//         if (string.IsNullOrEmpty(line))
//         {
//             // commit message divider
//             processingMessage = !processingMessage;
//         }

//         if (line.Length > 0 && line[0] == '\t')
//         {
//             // commit message.
//             commit.Message += line;
//         }

//         if (line.Length > 1 && Char.IsLetter(line[0]) && line[1] == '\t')
//         {
//             var status = line.Split('\t')[0];
//             var file = line.Split('\t')[1];
//             commit.Files.Add(new GitFileStatus() { Status = status, File = file });
//         }
//     }
//     while (strReader.Peek() != -1);
// }
// if (commit != null)
//     commits.Add(commit);

// return commits;