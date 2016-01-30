using System.Xml.Serialization;

namespace compiler
{
  public class EnumeratorModel
  {
    [XmlAttribute("name")]
    public string name { get; set; }
  }
}
