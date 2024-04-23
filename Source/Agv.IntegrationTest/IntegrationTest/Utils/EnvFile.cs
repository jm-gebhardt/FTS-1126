using System.Diagnostics;

namespace Gebhardt.Acc.IntegrationTest.Utils
{
    /// <summary>
    /// Generates Environment file from template
    /// </summary>
    public class EnvFile
    {
        /// <summary>
        /// File path of generated environment file
        /// </summary>
        public string? FilePath;

        /// <summary>
        /// Default user
        /// </summary>
        public const string DefaultUser = "user";

        /// <summary>
        /// Default password
        /// </summary>
        public const string DefaultPassword = "agvs!Ar3Aw3som3";

        private readonly string _composeDirectoryPath;
        private string EnvTemplateFile => $"{_composeDirectoryPath}/.env.template";
        private Dictionary<string, string> _dict = new();

        /// <summary>
        /// EnvFile Constructor
        /// </summary>
        /// <param name="composeFilePath"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public EnvFile(string? composeFilePath = null)
        {
            _composeDirectoryPath =
                Path.GetDirectoryName(composeFilePath)
                ?? throw new InvalidOperationException($"Environment variable {EnvVars.ComposeFileEnvVar} must be set");
        }

        /// <summary>
        /// Generates Env file based on template
        /// Variables are generated as follows:
        /// *_USER => user
        /// *_PASSWORD => agvs!Ar3Aw3som3
        /// all others will be left empty
        /// <returns>File path of generated env file</returns>
        /// </summary>
        public string Create()
        {
            if (this.FilePath != null)
            {
                return FilePath;
            }

            var envTemplate = File.ReadAllLines(EnvTemplateFile);
            _dict = envTemplate
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
                .Select(line => line.Split('='))
                .ToDictionary(split => split[0], split => split[1]);

            foreach (var key in _dict.Keys)
            {
                _dict[key] = key.Split("_").Last() switch
                {
                    "USER" => DefaultUser,
                    "PASSWORD" => DefaultPassword,
                    _ => _dict[key]
                };
            }

            FilePath = Path.GetTempFileName();
            Trace.WriteLine($"FilePath for .env file: {FilePath}");
            File.WriteAllLines(FilePath, from kvp in _dict select kvp.Key + "=" + kvp.Value);

            return FilePath;
        }
    }
}