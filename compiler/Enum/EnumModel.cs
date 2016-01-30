using System.Collections.Generic;
using System.Xml.Serialization;

namespace compiler
{
  [XmlRoot("enum")]
  public class EnumModel
  {
    [XmlAttribute("name")]
    public string name { get; set; }

    [XmlElement("enumerator")]
    public List<EnumeratorModel> enumerators { get; set; }
  }
}
