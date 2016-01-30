using System.Xml.Serialization;

namespace compiler
{
  public class ArgModel
  {
    [XmlAttribute("type")]
    public string type { get; set; }

    [XmlAttribute("name")]
    public string name { get; set; }
  }
}
