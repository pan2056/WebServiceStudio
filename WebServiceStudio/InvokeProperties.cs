namespace WebServiceStudio
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class InvokeProperties
    {
        private ArrayList uris = new ArrayList();
        private string certname;

        public void AddUri(string uri)
        {
            this.uris.Remove(uri);
            this.uris.Insert(0, uri);
            Configuration.SaveMasterConfig();
        }

        public override string ToString()
        {
            return "";
        }

        [XmlArrayItem("Uri", typeof(string)), Browsable(false)]
        public string[] RecentlyUsedUris
        {
            get
            {
                return (this.uris.ToArray(typeof(string)) as string[]);
            }
            set
            {
                this.uris.Clear();
                if (value != null)
                {
                    this.uris.AddRange(value);
                }
            }
        }

        [Browsable(false), XmlElement("CertName")]
        public string CertName
        {
            get
            {
                return this.certname;
            }

            set
            {
                certname = value;
            }
        }

    }
}

