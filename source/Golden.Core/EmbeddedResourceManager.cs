namespace Golden
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Represents a resource manager that provides convenient access to embedded resources at run time.
    /// </summary>
    public class EmbeddedResourceManager
    {
        private readonly Assembly _Assembly;
        private readonly string _BaseName;
        private readonly Lazy<string[]> _ResourceKeys;

        protected Assembly Assembly
        {
            get { return _Assembly; }
        }
        public string BaseName
        {
            get { return _BaseName; }
        }
        public string[] ResourceKeys
        {
            get { return _ResourceKeys.Value; }
        }

        public EmbeddedResourceManager() : this("")
        {
        }
        public EmbeddedResourceManager(string baseName) : this(Assembly.GetCallingAssembly(), baseName)
        {
        }
        public EmbeddedResourceManager(Type resourceSource, string baseName = null) : this(resourceSource.Assembly, baseName)
        {
        }
        public EmbeddedResourceManager(Assembly assembly, string baseName = null)
        {
            _Assembly = assembly;
            _BaseName = baseName;
            _ResourceKeys = new Lazy<string[]>(() => _Assembly.GetManifestResourceNames());
        }
        protected string GetFullResourceKey(string resourceKey)
        {
            resourceKey = resourceKey.Replace('/', '.').Replace('\\', '.');
            if (string.IsNullOrEmpty(_BaseName)) return resourceKey;
            if (string.IsNullOrEmpty(resourceKey)) return _BaseName;
            return string.Concat(_BaseName, ".", resourceKey);
        }
        /// <summary>
        /// Gets specified resource file content as text (UTF-8).
        /// </summary>
        /// <param name="resourceKey">The case-sensitive resource name</param>
        public string GetFileAsText(string resourceKey)
        {
            return GetFileAsText(resourceKey, Encoding.UTF8);
        }
        /// <summary>
        /// Gets specified resource file content as text.
        /// </summary>
        /// <param name="resourceKey">The case-sensitive resource name</param>
        public string GetFileAsText(string resourceKey, Encoding encoding)
        {
            using (var reader = new StreamReader(GetStream(resourceKey), encoding))
            {
                var result = reader.ReadToEnd();
                reader.Close();
                return result;
            }
        }
        /// <summary>
        /// Gets specified resource bytes.
        /// </summary>
        /// <param name="resourceKey">The case-sensitive resource name</param>
        public byte[] GetBytes(string resourceKey)
        {
            resourceKey = GetFullResourceKey(resourceKey);
            using (var resStrm = this.GetStream(resourceKey))
            {
                var bytes = new byte[resStrm.Length];
                resStrm.Read(bytes, 0, bytes.Length);
                resStrm.Close();
                return bytes;
            }
        }
        /// <summary>
        /// Determines whether this assembly contains a specified resource.
        /// </summary>
        /// <param name="resourceKey">The case-sensitive resource name</param>
        public bool Contains(string resourceKey)
        {
            resourceKey = GetFullResourceKey(resourceKey);
            return ResourceKeys.Contains(resourceKey, StringComparer.Ordinal);
        }
        /// <summary>
        /// Gets specified resource stream.
        /// </summary>
        /// <param name="resourceKey">The case-sensitive resource name</param>
        public Stream GetStream(string resourceKey)
        {
            resourceKey = GetFullResourceKey(resourceKey);
            return _Assembly.GetManifestResourceStream(resourceKey);
        }
        public string[] FindFolderResourceNames(string folderKey)
        {
            if (this.ResourceKeys.Length == 0) return new string[0];
            folderKey = GetFullResourceKey(folderKey);
            if (string.IsNullOrEmpty(folderKey)) return (string[])this.ResourceKeys.Clone();
            folderKey = string.Concat(folderKey, ".");
            return this.ResourceKeys.Where(k => k.StartsWith(folderKey, StringComparison.Ordinal)).ToArray();
        }
    }
}
