using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Autodesk.ADN.Toolkit.ViewData.DataContracts
{
    public class MetaData
    {
        [JsonProperty(PropertyName = "childPath")]
        public string ChildPath
        {
            get;
            private set;
        }

        [JsonProperty(PropertyName = "parentPath")]
        public string ParentPath
        {
            get;
            private set;
        }

        public MetaData(string parent, string child)
        {
            ChildPath = child;
            ParentPath = parent;
        }
    }

    public class ReferenceDependency
    {
        [JsonProperty(PropertyName = "file")]
        public string File
        {
            get;
            private set;
        }

        [JsonProperty(PropertyName = "metadata")]
        public MetaData MetaData
        {
            get;
            private set;
        }

        public ReferenceDependency(
            string fileId, 
            MetaData data)
        {
            File = fileId;

            MetaData = data;
        }
    }

    public class ReferenceData
    {
        [JsonProperty(PropertyName = "master")]
        public string Master
        {
            get;
            private set;
        }

        [JsonProperty(PropertyName = "dependencies")]
        public List<ReferenceDependency> Dependencies
        {
            get;
            private set;
        }

        private string GetPath(string fileId)
        {
            var result = fileId.Split(new char[] { '/' });

            if (result.Length > 1)
                return result[1];

            return string.Empty;
        }

        public ReferenceData(
            string masterFileId, 
            List<string> dependenciesFileId) 
        {
            Master = masterFileId;

            Dependencies = new List<ReferenceDependency>();

            foreach (var fileId in dependenciesFileId)
            { 
                var metadData = new MetaData(
                    GetPath(masterFileId), 
                    GetPath(fileId));

                var dependency = new ReferenceDependency(
                    fileId,
                    metadData);

                Dependencies.Add(dependency);
            }
        }
    }

    public class ReferenceResponse : ViewDataResponseBase
    {
        [JsonConstructor]
        public ReferenceResponse()
        { 
        
        }
    }
}