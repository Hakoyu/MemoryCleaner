using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace HKW.FileIni
{
    /// <summary>Ini章节集</summary>
    public class IniSectionsSet : Dictionary<string, IniKeyValuesSet>
    {
        public IniSectionsSet() { }
        /// <summary>初始化并添加章节</summary>
        /// <param name="section">章节</param>
        public IniSectionsSet(string section) => TryAdd(section, new());
        /// <summary>初始化并添加章节键值</summary>
        /// <param name="section">章节键值</param>
        public IniSectionsSet(IniSection section) => TryAdd(section.Section, section.KeyValues);
        /// <summary>初始化并添加多个章节键值</summary>
        /// <param name="iniSectionsSet">章节键值集</param>
        public IniSectionsSet(IniSectionsSet iniSectionsSet)
        {
            foreach (var section in iniSectionsSet)
                Add(section.Key, section.Value);
        }
        /// <summary>添加章节</summary>
        /// <param name="section">章节</param>
        /// <returns>成功为true,失败为false</returns>
        public bool Add(string section) => TryAdd(section, new());
        /// <summary>添加章节键值</summary>
        /// <param name="section">章节键值</param>
        /// <returns>成功为true,失败为false</returns>
        public bool Add(IniSection section) => TryAdd(section.Section, section.KeyValues);
        /// <summary>添加章节键值集</summary>
        /// <param name="iniSectionsSet">章节键值集</param>
        /// <returns>成功添加的次数</returns>
        public int AddRange(IniSectionsSet iniSectionsSet)
        {
            int success = 0;
            foreach (var section in iniSectionsSet)
                if (TryAdd(section.Key, section.Value))
                    success++;
            return success;
        }
    }
    /// <summary>Ini章节</summary>
    public class IniSection
    {
        /// <summary>章节</summary>
        public readonly string Section;
        /// <summary>键值集</summary>
        public readonly IniKeyValuesSet KeyValues;
        /// <summary>初始化章节</summary>
        /// <param name="section">章节</param>
        public IniSection(string section, IniKeyValue keyValue)
        {
            Section = section;
            KeyValues = new() { keyValue };
        }
        /// <summary>初始化并添加章节及键值集</summary>
        /// <param name="section">章节</param>
        /// <param name="keyValues">键值集</param>
        public IniSection(string section, IniKeyValuesSet keyValues)
        {
            Section = section;
            KeyValues = keyValues;
        }
    }
    /// <summary>Ini键值集</summary>
    public class IniKeyValuesSet : Dictionary<string, IniValuesSet>
    {
        public IniKeyValuesSet() { }
        /// <summary>初始化并添加键值</summary>
        /// <param name="keyValue">键值</param>
        public IniKeyValuesSet(IniKeyValue keyValue) => Add(keyValue.Key, keyValue.Values);
        /// <summary>初始化并添加键值集</summary>
        /// <param name="keyValues">键值集</param>
        public IniKeyValuesSet(IniKeyValuesSet keyValues)
        {
            foreach (var kvs in keyValues)
                Add(kvs.Key, kvs.Value);
        }
        /// <summary>添加键值</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>成功为true,失败为false</returns>
        public bool Add(string key, string value) => TryAdd(key, new() { value });
        /// <summary>添加键值</summary>
        /// <param name="keyValue">键值</param>
        /// <returns>成功为true,失败为false</returns>
        public bool Add(IniKeyValue keyValue) => TryAdd(keyValue.Key, keyValue.Values);
        /// <summary>添加键值集</summary>
        /// <param name="iniKeyValues">键值集</param>
        /// <returns>成功添加的次数</returns>
        public int AddRange(IniKeyValuesSet iniKeyValues)
        {
            int success = 0;
            foreach (var kvs in iniKeyValues)
                if (TryAdd(kvs.Key, kvs.Value))
                    success++;
            return success;
        }
        public bool ReplaceKeyName(string oldKeyName, string newKeyName)
        {
            if (ContainsKey(oldKeyName))
            {
                IniKeyValuesSet temp = new();
                foreach (var kvs in this)
                {
                    if (kvs.Key.Equals(oldKeyName))
                        temp.Add(newKeyName, kvs.Value);
                    else
                        temp.Add(kvs.Key, kvs.Value);
                }
                Clear();
                AddRange(temp);
                return true;
            }
            return false;
        }
    }
    /// <summary>Ini键值</summary>
    public class IniKeyValue
    {
        /// <summary>键</summary>
        public readonly string Key;
        /// <summary>值集</summary>
        public readonly IniValuesSet Values;
        /// <summary>初始化并添加键</summary>
        /// <param name="key">键</param>
        public IniKeyValue(string key)
        {
            Key = key;
            Values = new();
        }
        /// <summary>初始化并添加键值</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public IniKeyValue(string key, string value)
        {
            Key = key;
            Values = new() { value };
        }
        /// <summary>初始化并添加键和值集</summary>
        /// <param name="key">键</param>
        /// <param name="values">值集</param>
        public IniKeyValue(string key, IniValuesSet values)
        {
            Key = key;
            Values = values;
        }
    }
    /// <summary>ini值集</summary>
    public class IniValuesSet : HashSet<string>
    {
        public IniValuesSet() { }
        /// <summary>初始化并添加值</summary>
        public IniValuesSet(string value) => Add(value);
        /// <summary>初始化并添加值集</summary>
        public IniValuesSet(IniValuesSet values)
        {
            foreach (string value in values)
                Add(value);
        }
        new public bool Add(string value) => base.Add(value);
        /// <summary>添加值集</summary>
        public bool AddRange(IniValuesSet values) => AddRange(values);
        /// <summary>替换键值</summary>
        public void Replace(string value)
        {
            Clear();
            Add(value);
        }
        /// <summary>替换键值</summary>
        public void Replace(IniValuesSet values)
        {
            Clear();
            AddRange(values);
        }

    }
    /// <summary>ini文件解析</summary>
    class FileIni : IDisposable
    {
        public FileIni(string path)
        {
            filePath = path;
            iniDataSet = InitializeAllData();
        }
        ~FileIni()
        {
            Dispose(false);
        }
        /// <summary>关闭文件</summary>
        public void Close() => Dispose(true);
        /// <summary>清空资源</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>清空资源</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Save();
                iniDataSet = null!;
            }
        }
        ///<summary>数据集</summary>
        IniSectionsSet iniDataSet = null!;
        /// <summary>文件路径</summary>
        private string filePath = null!;
        private readonly Regex matchSection = new(@"^\[[\S\s]+\]$");
        private readonly Regex matchKeyValue = new(@"^[\S\s]+=(|[\S\s])+$");
        /// <summary>载入数据</summary>
        private IniSectionsSet InitializeAllData()
        {
            IniSectionsSet iniDataset = new();
            using (StreamReader sr = new(filePath))
            {
                string section = "";
                for (string line; (line = sr.ReadLine()!) != null;)
                {
                    if (matchSection.IsMatch(line))
                    {
                        section = DeconstructSection(line);
                        if (!iniDataset.TryAdd(section, new()))
                            throw new ArgumentNullException(ToString(), "Duplicate sections detected");
                    }
                    else if (matchKeyValue.IsMatch(line))
                    {
                        string[] kv = line.Split('=');
                        if (!iniDataset[section].TryAdd(kv[0], new() { string.Join("=", kv[1..]) }))
                            iniDataset[section][kv[0]].Add(string.Join("=", kv[1..]));
                    }
                }
            }
            return iniDataset;
        }
        public IniKeyValuesSet? this[string section]
        {
            get
            {
                if (iniDataSet.ContainsKey(section))
                    return iniDataSet[section];
                else
                    return null;
            }
        }
        public IniValuesSet? this[string section, string key]
        {
            get
            {
                if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(key))
                    return iniDataSet[section][key];
                else
                    return null;
            }
        }
        /// <summary>获取所有章节名称</summary>
        /// <returns>所有章节名称</returns>
        public List<string> GetAllSections()
        {
            return (from section in iniDataSet.AsParallel().AsOrdered()
                    select section.Key).ToList();
        }
        /// <summary>获取章节内的所有键值</summary>
        /// <param name="section">章节</param>
        /// <returns>章节内所有键值</returns>
        public IniKeyValuesSet? GetAllKeyValue(string section)
        {
            if (iniDataSet.ContainsKey(section))
                return new(iniDataSet[section]);
            return null;
        }
        /// <summary>获取章节内键的所有值</summary>
        /// <param name="section">章节</param>
        /// <param name="key">键</param>
        /// <returns>章节内键的所有值</returns>
        public List<string>? GetKeyAllValues(string section, string key)
        {
            if (iniDataSet.ContainsKey(section))
                if (iniDataSet[section].ContainsKey(key))
                    return new(iniDataSet[section][key]);
            return null;
        }
        /// <summary>添加章节</summary>
        /// <param name="section">章节</param>
        /// <returns>成功为true,失败为false</returns>
        public bool AddSection(string section) => iniDataSet.TryAdd(section, new());
        /// <summary>添加键值</summary>
        /// <param name="section">章节</param>
        /// <param name="keyValue">键值</param>
        /// <returns>成功为true,失败为false</returns>
        public bool AddKeyValue(string section, IniKeyValue keyValue)
        {
            if (iniDataSet.ContainsKey(section))
                return iniDataSet[section].Add(keyValue);
            return false;
        }
        /// <summary>添加键值集</summary>
        /// <param name="section">章节</param>
        /// <param name="keyValues">键值集</param>
        /// <returns>成功添加的数量</returns>
        public int AddKeyValuesSet(string section, IniKeyValuesSet keyValues)
        {
            if (iniDataSet.ContainsKey(section))
                return iniDataSet[section].AddRange(keyValues);
            return 0;
        }
        /// <summary>添加值</summary>
        /// <param name="section">章节</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>成功为true,失败为false</returns>
        public bool AddValue(string section, string key, string value)
        {
            if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(key))
                iniDataSet[section][key].Add(value);
            return false;
        }
        /// <summary>添加值集</summary>
        /// <param name="section">章节</param>
        /// <param name="key">键</param>
        /// <param name="values">值集</param>
        /// <returns>成功为true,失败为false</returns>
        public bool AddValues(string section, string key, IniValuesSet values)
        {
            if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(key))
                iniDataSet[section][key].AddRange(values);
            return false;
        }
        /// <summary>替换章节名称</summary>
        /// <param name="oldSectionName">旧章节名称</param>
        /// <param name="newSectionName">新章节名称</param>
        /// <returns>成功为true,失败为false</returns>
        public bool ReplaceSectionName(string oldSectionName, string newSectionName)
        {
            if (iniDataSet.ContainsKey(oldSectionName) && !iniDataSet.ContainsKey(newSectionName))
            {
                IniSectionsSet temp = new();
                foreach (var section in iniDataSet)
                {
                    if (section.Key.Equals(oldSectionName))
                        temp.Add(newSectionName, section.Value);
                    else
                        temp.Add(section.Key, section.Value);
                }
                iniDataSet = temp;
            }
            return false;
        }
        /// <summary>替换章节内键名称/summary>
        /// <param name="section">章节</param>
        /// <param name="oldKeyValues">旧键名称</param>
        /// <param name="newKeyValues">新键名称</param>
        /// <returns>成功为true,失败为false</returns>
        public bool ReplaceKeyName(string section, string oldKeyName, string newKeyName)
        {
            if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(oldKeyName))
            {
                IniKeyValuesSet temp = new();
                foreach (var kvs in iniDataSet[section])
                {
                    if (kvs.Key.Equals(oldKeyName))
                        temp.Add(newKeyName, kvs.Value);
                    else
                        temp.Add(kvs.Key, kvs.Value);
                }
                iniDataSet[section] = temp;
            }
            return false;
        }
        /// <summary>替换章节内键名称/summary>
        /// <param name="section">章节</param>
        /// <param name="oldKeyValues">旧键名称</param>
        /// <param name="newKeyValues">新键名称</param>
        /// <returns>成功为true,失败为false</returns>
        public bool ReplaceKeyValue(string section, string key, IniValuesSet values)
        {
            if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(key))
                iniDataSet[section][key] = values;
            return false;
        }
        /// <summary>删除章节及其键值集/summary>
        /// <param name="section">章节</param>
        /// <returns>成功为true,失败为false</returns>
        public bool RemoveSection(string section)
        {
            if (iniDataSet.ContainsKey(section))
                iniDataSet.Remove(section);
            return false;
        }
        /// <summary>删除指定章节中的键及其值集</summary>
        /// <param name="section">章节</param>
        /// <param name="key">键</param>
        /// <returns>成功为true,失败为false</returns>
        public bool RemoveKey(string section, string key)
        {
            if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(key))
                iniDataSet[section].Remove(key);
            return false;
        }
        /// <summary>删除指定章节中的键包含的所有值</summary>
        /// <param name="section">章节</param>
        /// <param name="key">键</param>
        /// <returns>成功为true,失败为false</returns>
        public bool RemoveValueInKey(string section, string key)
        {
            if (iniDataSet.ContainsKey(section) && iniDataSet[section].ContainsKey(key))
                iniDataSet[section].Remove(key);
            return false;
        }
        /// <summary>保存数据</summary>
        public void Save()
        {
            using StreamWriter sw = new(filePath, false);
            foreach (var section in iniDataSet)
            {
                sw.WriteLine(BuildSection(section.Key));
                foreach (var kv in section.Value)
                    foreach (string v in kv.Value)
                        sw.WriteLine($"{kv.Key}={v}");
                sw.WriteLine("");
            }
        }
        /// <summary>构建节</summary>
        private string BuildSection(string section) => $"[{section}]";
        /// <summary>解构节</summary>
        private string DeconstructSection(string section) => section[1..^1];
    }
}