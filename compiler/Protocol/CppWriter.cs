using System;
using System.IO;
using System.Collections.Generic;

namespace compiler
{
  class CppWriter
  {
    string flow_;
    string file_name;
    string class_name;
    StreamWriter writer_;
    ProtocolModel model_;

    public CppWriter(string flow, ProtocolModel model)
    {
      flow_ = flow;

      // file_name.
      if (flow_ == "s2c")
        file_name = model.srv_name + '2' + model.cli_name;
      else
        file_name = model.cli_name + '2' + model.srv_name;

      // class_name.
      bool is_upper = true;
      foreach (var c in file_name)
      {
        if (c == '_')
        {
          is_upper = true;
          continue;
        }
        if (c == '2')
        {
          is_upper = true;
          class_name += c;
          continue;
        }
        if (is_upper)
        {
          class_name += char.ToUpper(c);
          is_upper = false;
          continue;
        }
        class_name += c;
      }

      model_ = model;
    }

    public void Exec()
    {
      // ファイル作成.
      writer_ = new StreamWriter(file_name + ".cpp");

      // インクルード.
      writer_.WriteLine("#include<arpa/inet.h>");
      writer_.WriteLine("#include<string.h>");
      writer_.WriteLine("#include\"" + file_name + ".h\"");

      // 定数定義.
      string enum_name = class_name + "Enum";
      writer_.WriteLine("enum class " + enum_name + '{');

      foreach (var func in model_.funcs)
        writer_.WriteLine('k' + func.name + ',');

      writer_.WriteLine("};");

      // staticメンバ.
      if (flow_ == "s2c")
        WriteMember("c2s");
      else
        WriteMember("s2c");

      // RPC関数定義.
      WriteRecv(enum_name);
      WriteClear();

      // 送信関数定義.
      WriteCall(enum_name);

      // バッファフラッシュ.
      writer_.Flush();
    }

    void WriteClear()
    {
      writer_.WriteLine("void " + class_name + "::Clear(){");
      writer_.WriteLine("len_=0;");
      writer_.WriteLine('}');
    }

    void WriteMember(string flow)
    {
      foreach (var func in model_.funcs)
      {
        // flow識別.
        if (func.flow != flow) continue;

        writer_.Write("void (*" + class_name + "::" + func.name + "_)(");
        WriteArg(func);
        writer_.WriteLine(")=nullptr;");
      }
    }

    public void WriteArg(FuncModel func)
    {
      var args = new List<string>();
      // 引数.
      foreach (var arg in func.args)
      {
        // 文字列.
        if (arg.type == "string")
        {
          args.Add("const char*" + arg.name);
          continue;
        }
        args.Add("const " + GetType(arg) + ' ' + arg.name);
      }
      writer_.Write(String.Join(",", args));
    }

    public string GetType(ArgModel arg)
    {
      if (arg.type == "string")
        return "char*";

      if (arg.type == "float")
        return "float";

      string str = "";
      if (arg.type[0] == 'u')
        str += 'u';
      if (arg.type == "byte")
        str += 'u';

      return str + "int" + GetByteNum(arg) * 8 + "_t";
    }

    void WriteCall(string enum_name)
    {
      foreach (var func in model_.funcs)
      {
        // 送信関数のみ処理.
        if (func.flow != flow_) continue;

        writer_.WriteLine("void " + class_name + "::" + func.name + '(');
        WriteArg(func);
        writer_.WriteLine("){");

        // ID.
        writer_.WriteLine('{');
        writer_.WriteLine("auto id=(int16_t)" + enum_name + "::k" + func.name + ';');
        writer_.WriteLine("id=htons(id);");
        writer_.WriteLine("memcpy(&buf_[len_],&id,sizeof(id));");
        writer_.WriteLine("len_+=sizeof(id);");
        writer_.WriteLine('}');

        // 引数をバッファに.
        foreach (var arg in func.args)
        {
          if (arg.type == "string")
          {
            writer_.WriteLine("strcpy((char*)&buf_[len_]," + arg.name + ");");
            writer_.WriteLine("len_+=strlen(" + arg.name + ")+1;");
            continue;
          }
          writer_.Write("auto arg___" + arg.name + '=');
          if (arg.type.Contains("byte"))
          {
            writer_.WriteLine(arg.name + ';');
          }
          else if (arg.type.Contains("short"))
          {
            writer_.WriteLine("htons(" + arg.name + ");");
          }
          else if (arg.type.Contains("int"))
          {
            writer_.WriteLine("htonl(" + arg.name + ");");
          }
          else if (arg.type == "float")
          {
            writer_.WriteLine("htonl(*(int32_t*)&" + arg.name + ");");
          }
          writer_.WriteLine("memcpy(&buf_[len_],&arg___" + arg.name + ",sizeof(" + arg.name + "));");
          writer_.WriteLine("len_+=sizeof(" + arg.name + ");");
        }

        writer_.WriteLine('}');
      }
    }

    void WriteRecv(string enum_name)
    {
      writer_.WriteLine("void " + class_name + "::Recv(const int8_t*buf,const int32_t len){");
      writer_.WriteLine("for(int32_t cnt=0;cnt!=len;){");
      // まずはデータ抜き出し.
      writer_.WriteLine("auto id=ntohs(*(int16_t*)&buf[cnt]);");
      writer_.WriteLine("cnt+=sizeof(id);");
      writer_.WriteLine("switch((" + enum_name + ")id){");
      foreach (var func in model_.funcs)
      {
        // 受信関数のみ処理.
        if (func.flow == flow_) continue;

        writer_.WriteLine("case " + enum_name + "::k" + func.name + ":{");

        WriteGetArg(func);
        WriteCallFunc(func);

        writer_.WriteLine("}break;");
      }
      writer_.WriteLine('}');
      writer_.WriteLine('}');
      writer_.WriteLine('}');
    }

    public int GetByteNum(ArgModel arg)
    {
      if (arg.type == "string")
        return -1;
      if (arg.type.Contains("byte"))
        return 1;
      if (arg.type.Contains("short"))
        return 2;
      if (arg.type.Contains("int"))
        return 4;
      if (arg.type == "float")
        return 4;

      return 0;
    }

    void WriteGetArg(FuncModel func)
    {
      foreach (var arg in func.args)
      {
        writer_.Write("auto " + arg.name + '=');

        // 文字列.
        if (arg.type == "string")
        {
          writer_.WriteLine("(char*)&buf[cnt];");
          writer_.WriteLine("cnt+=strlen(" + arg.name + ")+1;");
          continue;
        }
        else if (arg.type.Contains("byte"))
        {
          writer_.WriteLine("*(" + GetType(arg) + "*)&buf[cnt];");
        }
        else if (arg.type.Contains("short"))
        {
          writer_.WriteLine('(' + GetType(arg) + ")ntohs(*(int16_t*)&buf[cnt]);");
        }
        else if (arg.type.Contains("int"))
        {
          writer_.WriteLine('(' + GetType(arg) + ")ntohl(*(int32_t*)&buf[cnt]);");
        }
        else if (arg.type.Contains("float"))
        {
          writer_.WriteLine("[&]()->float{auto work = ntohl(*(int32_t*)&buf[cnt]);return *(float*)&work;}();");
        }
        writer_.WriteLine("cnt+=sizeof(" + arg.name + ");");
      }
    }

    void WriteCallFunc(FuncModel func)
    {
      writer_.Write(func.name + "_(");
      List<string> args = new List<string>();
      foreach (var arg in func.args)
        args.Add(arg.name);
      writer_.Write(String.Join(",", args));
      writer_.WriteLine(");");
    }
  }
}
