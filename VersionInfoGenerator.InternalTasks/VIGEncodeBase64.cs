using System;
using System.Text;
using Microsoft.Build.Framework;

namespace VersionInfoGenerator.InternalTasks
{
    [LoadInSeparateAppDomain]
    public class VIGEncodeBase64 : MarshalByRefObject, ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public string Value { get; set; }

        [Output]
        public string EncodedValue { get; set; }

        public bool Execute()
        {
            var bytes = Encoding.UTF8.GetBytes(this.Value ?? "");
            this.EncodedValue = Convert.ToBase64String(bytes);
            return true;
        }
    }
}
