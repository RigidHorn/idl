using System;
using System.IO;
using System.Collections.Generic;

namespace compiler
{
  class CSharpWriter
  {
    string flow_;
    string class_name;
    StreamWriter writer_;
    ProtocolModel model_;

    public CSharpWriter(string flow, ProtocolModel model)
    {
      flow_ = flow;

      // class_name.
      string work_str;
      if (flow_ == "s2c")
        work_str = model.srv_name + '2' + model.cli_name;
      else
        work_str = model.cli_name + '2' + model.srv_name;
      bool is_upper = true;
      foreach (var c in work_str)
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
      writer_ = new StreamWriter(class_name + ".cs");

      // using.
      writer_.WriteLine("using System;");
      writer_.WriteLine("using System.Text;");
      writer_.WriteLine("using System.Net;");

      // class.
      writer_.WriteLine("public class " + class_name);
      writer_.WriteLine('{');

      // 定数定義.
      var enum_name = class_name + "Enum";
      writer_.WriteLine("enum " + enum_name);
      writer_.WriteLine('{');
      foreach (var func in model_.funcs)
        writer_.WriteLine('k' + func.name + ',');
      writer_.WriteLine('}');

      // RPC関数.
      writer_.WriteLine("public int len{get{return len_;}}");
      writer_.WriteLine("public byte[]buf{get{return buf_;}}");
      writer_.WriteLine("public void Clear(){len_=0;}");

      // delegate.
      WriteDelegate();

      // Recv.
      WriteRecv(enum_name);

      // Call.
      WriteCall(enum_name);

      // メンバ.
      writer_.WriteLine("int len_=0;");
      writer_.WriteLine("byte[]buf_=new byte[" + model_.buf_size + "];");

      writer_.WriteLine('}');

      // バッファフラッシュ.
      writer_.Flush();
    }

    void WriteDelegate()
    {
      foreach (var func in model_.funcs)
      {
        // 受信関数のみ処理.
        if (func.flow == flow_) continue;

        writer_.Write("public delegate void " + func.name + "Delegate(");
        WriteArg(func);
        writer_.WriteLine(");");
      }
      foreach (var func in model_.funcs)
      {
        // 受信関数のみ処理.
        if (func.flow == flow_) continue;

        writer_.WriteLine("static " + func.name + "Delegate " + func.name + "_=null;");
        writer_.WriteLine("public static " + func.name + "Delegate " + func.name + "{set{" + func.name + "_=value;}}");
      }
    }

    void WriteArg(FuncModel func)
    {
      List<string> args = new List<string>();
      foreach (var arg in func.args)
      {
        args.Add(arg.type + ' ' + arg.name);
      }
      writer_.Write(String.Join(",", args));
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

    void WriteCall(string enum_name)
    {
      foreach (var func in model_.funcs)
      {
        // 送信関数のみ処理.
        if (func.flow != flow_) continue;

        writer_.Write("public void " + func.name + '(');
        WriteArg(func);
        writer_.WriteLine(')');
        writer_.WriteLine('{');

        // id.
        writer_.WriteLine("var func___id=(short)" + enum_name + ".k" + func.name + ';');
        writer_.WriteLine("func___id=IPAddress.HostToNetworkOrder(func___id);");
        writer_.WriteLine("var byte___func_id=BitConverter.GetBytes(func___id);");
        writer_.WriteLine("foreach(var byte___b in byte___func_id)");
        writer_.WriteLine("buf_[len_++]=byte___b;");

        foreach (var arg in func.args)
        {
          if (arg.type == "string")
          {
            writer_.WriteLine("var byte___" + arg.name + "=Encoding.UTF8.GetBytes(" + arg.name + "+'\\0');");
          }
          else if (arg.type.Contains("byte"))
          {
            writer_.WriteLine("byte[]byte___" + arg.name + "=new byte[1];");
            writer_.WriteLine("byte___" + arg.name + "[0]=(byte)" + arg.name + ";");
          }
          else if (arg.type == "float")
          {
            // 一度intにキャストする.
            writer_.WriteLine("var byte___" + arg.name + "=BitConverter.GetBytes(" + arg.name + ");");
            writer_.WriteLine("var int___" + arg.name + "=BitConverter.ToInt32(byte___" + arg.name + ",0);");

            // バイトオーダ.
            writer_.WriteLine("int___" + arg.name + "=IPAddress.HostToNetworkOrder(int___" + arg.name + ");");
            writer_.WriteLine("byte___" + arg.name + "=BitConverter.GetBytes(int___" + arg.name + ");");
          }
          else
          {
            string utype = arg.type;
            if (arg.type[0] == 'u')
              utype = arg.type.Substring(1);

            writer_.WriteLine(arg.name + "=(" + arg.type + ")IPAddress.HostToNetworkOrder((" + utype + ')' + arg.name + ");");
            writer_.WriteLine("var byte___" + arg.name + "=BitConverter.GetBytes(" + arg.name + ");");
          }
          writer_.WriteLine("foreach(var byte___b in byte___" + arg.name + ')');
          writer_.WriteLine("buf_[len_++]=byte___b;");
        }

        writer_.WriteLine('}');
      }
    }

    void WriteRecv(string enum_name)
    {
      writer_.WriteLine("public void Recv(byte[]buf,int len)");
      writer_.WriteLine('{');
      writer_.WriteLine("for(int cnt=0;cnt!=len;)");
      writer_.WriteLine('{');
      writer_.WriteLine("var func___id=BitConverter.ToInt16(buf,cnt);");
      writer_.WriteLine("cnt+=2;");
      writer_.WriteLine("func___id = IPAddress.NetworkToHostOrder(func___id);");
      writer_.WriteLine("switch((" + enum_name + ")func___id)");
      writer_.WriteLine('{');
      foreach (var func in model_.funcs)
      {
        // 受信関数のみ処理.
        if (func.flow == flow_) continue;

        writer_.WriteLine("case " + enum_name + ".k" + func.name + ':');
        writer_.WriteLine('{');

        foreach (var arg in func.args)
        {
          if (arg.type == "string")
          {
            writer_.WriteLine("string " + arg.name + "=\"\";");
            // str___startの寿命制限.
            writer_.WriteLine('{');
            // 文字の開始場所と文字数をカウント.
            writer_.WriteLine("int str___start=cnt;");
            writer_.WriteLine("for(;buf[cnt]!='\\0';cnt++);");
            // \0.
            writer_.WriteLine("cnt++;");
            writer_.WriteLine(arg.name + "+=Encoding.UTF8.GetString(buf,str___start,cnt-str___start-1);");
            writer_.WriteLine('}');
          }
          else if (arg.type.Contains("byte"))
          {
            writer_.WriteLine("var " + arg.name + "=(" + arg.type + ")buf[cnt];");
            writer_.WriteLine("cnt+=" + GetByteNum(arg) + ';');
          }
          else if (arg.type == "float")
          {
            // intとしてバイトオーダ.
            writer_.WriteLine("var int___" + arg.name + "=BitConverter.ToInt32(buf,cnt);");
            writer_.WriteLine("int___" + arg.name + "=IPAddress.NetworkToHostOrder(int___" + arg.name + ");");

            // バイト列に戻す.
            writer_.WriteLine("var byte___" + arg.name + "=BitConverter.GetBytes(int___" + arg.name + ");");

            writer_.WriteLine("var " + arg.name + "=BitConverter.ToSingle(byte___" + arg.name + ",0);");

            // cnt.
            writer_.WriteLine("cnt+=" + GetByteNum(arg) + ';');
          }
          else
          {
            if (arg.type[0] == 'u')
              writer_.WriteLine("var " + arg.name + "=BitConverter.ToUInt" + GetByteNum(arg) * 8 + "(buf,cnt);");
            else
              writer_.WriteLine("var " + arg.name + "=BitConverter.ToInt" + GetByteNum(arg) * 8 + "(buf,cnt);");

            writer_.WriteLine("cnt+=" + GetByteNum(arg) + ';');

            // バイトオーダ.
            string utype = arg.type;
            if (arg.type[0] == 'u')
              utype = arg.type.Substring(1);
            writer_.WriteLine(arg.name + "=(" + arg.type + ")IPAddress.NetworkToHostOrder((" + utype + ')' + arg.name + ");");
          }
        }

        writer_.Write(func.name + "_(");
        var args = new List<string>();
        foreach (var arg in func.args)
          args.Add(arg.name);
        writer_.Write(String.Join(",", args));
        writer_.WriteLine(");");

        writer_.WriteLine('}');
        writer_.WriteLine("break;");
      }
      writer_.WriteLine('}');
      writer_.WriteLine('}');
      writer_.WriteLine('}');
    }
  }
}
