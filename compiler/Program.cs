using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace compiler
{
  class Program
  {
    ProtocolModel protocol_model_;
    EnumModel enum_model_;

    static void Main(string[] args)
    {
      Program program = new Program();

      program.ReadXml();
      program.CheckModel();
      program.MakeFile();

      Console.WriteLine("コンパイルが正常に終了しました。");
      Console.ReadLine();
    }

    void ReadXml()
    {
      Console.WriteLine("パスを入力して下さい。");
      var path = Console.ReadLine();

      FileStream fs = null;

      try
      {
        fs = new FileStream(path, FileMode.Open);
      }
      catch (Exception e)
      {
        Err(e.Message);
      }

      bool is_serialized = false;

      try
      {
        XmlSerializer serializer = new XmlSerializer(typeof(ProtocolModel));

        protocol_model_ = (ProtocolModel)serializer.Deserialize(fs);

        is_serialized = true;
        Console.WriteLine("Protocolファイルのコンパイルを開始します。");
      }
      catch (Exception)
      {
        fs.Seek(0, SeekOrigin.Begin);
      }

      try
      {
        XmlSerializer serializer = new XmlSerializer(typeof(EnumModel));

        enum_model_ = (EnumModel)serializer.Deserialize(fs);

        is_serialized = true;
        Console.WriteLine("Enumファイルのコンパイルを開始します。");
      }
      catch (Exception)
      {
        fs.Seek(0, SeekOrigin.Begin);
      }

      if (!is_serialized)
      {
        Err("正しくないファイルです。");
      }
    }

    void Err(string str)
    {
      Console.WriteLine(str);
      Console.ReadLine();
      Environment.Exit(1);
    }

    void CheckModel()
    {
      if (protocol_model_ != null)
        CheckProtocolModel();
      if (enum_model_ != null)
        CheckEnumModel();
    }

    void CheckProtocolModel()
    {
      if (protocol_model_.srv_name == null)
        Err("srv_name == null.");
      if (protocol_model_.cli_name == null)
        Err("cli_name == null.");
      if (protocol_model_.buf_size == 0)
        Err("buf_size == 0.");

      foreach (var func in protocol_model_.funcs)
      {
        if (func.name == null)
          Err("func name == null.");
        if (func.flow == null)
          Err(func.name + " flow == null.");
        if (func.flow != "c2s" &&
          func.flow != "s2c")
          Err(func.name + " flow err.");

        foreach (var arg in func.args)
        {
          if (arg.type == null)
            Err(func.name + "::" + "type == null.");
          if (arg.type != "string" &&
            arg.type != "sbyte" &&
            arg.type != "byte" &&
            arg.type != "short" &&
            arg.type != "ushort" &&
            arg.type != "int" &&
            arg.type != "uint" &&
            arg.type != "float")
            Err(func.name + "::" + "type err.");
          if (arg.name == null)
            Err(func.name + "::" + "name == null.");
        }
      }
    }

    void CheckEnumModel()
    {
      if (enum_model_.name == null)
        Err("enum name == null.");

      foreach (var enumerator in enum_model_.enumerators)
        if (enumerator.name == null)
          Err("enumerator name == null.");
    }

    void MakeFile()
    {
      if (protocol_model_ != null)
        MakeProtocolFile();
      if (enum_model_ != null)
        MakeEnumFile();
    }

    void MakeProtocolFile()
    {
      HeaderWriter s2chw = new HeaderWriter("s2c", protocol_model_);
      HeaderWriter c2shw = new HeaderWriter("c2s", protocol_model_);
      s2chw.Exec();
      c2shw.Exec();
      CppWriter s2ccw = new CppWriter("s2c", protocol_model_);
      CppWriter c2scw = new CppWriter("c2s", protocol_model_);
      s2ccw.Exec();
      c2scw.Exec();
      CSharpWriter s2csw = new CSharpWriter("s2c", protocol_model_);
      CSharpWriter c2ssw = new CSharpWriter("c2s", protocol_model_);
      s2csw.Exec();
      c2ssw.Exec();
    }

    void MakeEnumFile()
    {
      EnumWriter ew = new EnumWriter(enum_model_);
      ew.Exec();
    }
  }
}
