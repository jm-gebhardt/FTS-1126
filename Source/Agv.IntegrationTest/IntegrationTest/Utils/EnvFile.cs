namespace Gebhardt.StoreWare.Agv.IntegrationTest.Utils
{
    /// <summary>
    /// Generates Environment file from tempalte
    /// </summary>
    public class EnvFile
    {
        /// <summary>
        /// File path of generated environment file
        /// </summary>
        public string? FilePath = null;

        /// <summary>
        /// Default user
        /// </summary>
        public const string DefaultUser = "user";

        /// <summary>
        /// Default password
        /// </summary>
        public const string DefaultPassword = "agvs!Ar3Aw3som3";

        private readonly string _composeDirectoryPath;
        private string _envTemplateFile => $"{_composeDirectoryPath}/.env.template";
        private Dictionary<string, string> _dict = new();

        /// <summary>
        /// EnvFile Constructor
        /// </summary>
        /// <param name="composeFilePath"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public EnvFile(string? composeFilePath = null)
        {
            _composeDirectoryPath = Path.GetDirectoryName(composeFilePath ?? Environment.GetEnvironmentVariable(EnvVars.ComposeFileEnvVar)) 
                                    ?? throw new InvalidOperationException($"Environment variable {EnvVars.ComposeFileEnvVar} must be set");
        }

        /// <summary>
        /// Get env value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            return _dict[key];
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
            var envTemplate = File.ReadAllLines(_envTemplateFile);
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
            File.WriteAllLines(FilePath, from kvp in _dict select kvp.Key + "=" + kvp.Value);
            
            return FilePath;
        }
        
    }
}