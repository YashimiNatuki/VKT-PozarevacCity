using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CitySaveStore
    {
        [Serializable] private sealed class Envelope { public int version; public string payload; public string sha256; }
        private readonly string path;
        private bool preserveDamagedPrimary;
        private string expectedFingerprint;
        public bool CanWrite { get; private set; } = true;
        public bool HasSave => File.Exists(path) || File.Exists(path + ".bak");
        public string PathName => path;
        public CitySaveStore(string filePath) { path = Path.GetFullPath(filePath ?? throw new ArgumentNullException(nameof(filePath))); }

        public bool TryLoad(out CityProfileData data, out string status)
        {
            data = null;
            if (!HasSave) { expectedFingerprint = null; status = "New profile"; return false; }
            try
            {
                using (FileStream gate = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    expectedFingerprint = Fingerprint(path);
                    return LoadLocked(out data, out status);
                }
            }
            catch (Exception error) when (IsStorageError(error))
            { CanWrite = false; status = "Profile is in use or inaccessible; existing files preserved."; return false; }
        }

        private bool LoadLocked(out CityProfileData data, out string status)
        {
            data = null;
            status = "New profile";
            CanWrite = true;
            preserveDamagedPrimary = false;
            if (!HasSave) return false;
            try
            {
                data = Read(path);
                status = "Profile restored";
                return true;
            }
            catch (NotSupportedException) { CanWrite = false; status = "Newer save format preserved; automatic writes disabled."; return false; }
            catch (Exception error) when (IsStorageError(error)) { }
            try
            {
                data = Read(path + ".bak");
                preserveDamagedPrimary = File.Exists(path);
                status = "Backup restored; damaged primary will be preserved before the next save.";
                return true;
            }
            catch (Exception error) when (IsStorageError(error) || error is NotSupportedException)
            {
                CanWrite = false;
                status = "Save could not be read. Existing files preserved; automatic writes disabled.";
                return false;
            }
        }

        public void Save(CityProfileData profile)
        {
            if (!CanWrite) throw new InvalidOperationException("Writes disabled to protect an unreadable or newer save.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream gate = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                if (Fingerprint(path) != expectedFingerprint) throw new InvalidOperationException("Profile changed in another session; reload before saving.");
                SaveLocked(profile);
            }
        }

        private void SaveLocked(CityProfileData profile)
        {
            if (!CanWrite) throw new InvalidOperationException("Writes disabled to protect an unreadable or newer save.");
            CityProgression.Validate(profile);
            string payload = JsonUtility.ToJson(profile);
            string json = JsonUtility.ToJson(new Envelope { version = 1, payload = payload, sha256 = Hash(payload) });
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (preserveDamagedPrimary)
                {
                    string suffix = ".recovery-" + Guid.NewGuid().ToString("N");
                    File.Copy(path, path + suffix);
                    if (File.Exists(path + ".bak")) File.Copy(path + ".bak", path + suffix + ".bak");
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                preserveDamagedPrimary = false;
                expectedFingerprint = Hash(json);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static CityProfileData Read(string source)
        {
            if (new FileInfo(source).Length > 65536) throw new InvalidDataException("Save exceeds its size limit.");
            Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(source, Encoding.UTF8));
            if (envelope == null || envelope.version <= 0) throw new InvalidDataException("Invalid save envelope.");
            if (envelope.version != 1) throw new NotSupportedException("Unsupported save format.");
            if (envelope.payload == null || envelope.sha256 != Hash(envelope.payload)) throw new InvalidDataException("Save checksum mismatch.");
            CityProfileData profile = JsonUtility.FromJson<CityProfileData>(envelope.payload);
            if (profile != null && profile.version > CityProgression.CurrentProfileVersion) throw new NotSupportedException("Newer profile format.");
            CityProgression.Validate(profile);
            return profile;
        }

        private static bool IsStorageError(Exception error) => error is IOException || error is UnauthorizedAccessException || error is ArgumentException;
        private static string Fingerprint(string file)
        {
            if (!File.Exists(file)) return null;
            if (new FileInfo(file).Length > 65536) throw new InvalidDataException("Save exceeds its size limit.");
            return Hash(File.ReadAllText(file, Encoding.UTF8));
        }
        private static string Hash(string input)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
        }
    }
}
