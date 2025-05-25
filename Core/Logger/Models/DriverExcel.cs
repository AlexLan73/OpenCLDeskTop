namespace Logger.Models;
public record DataList(string TypeData, object Data);
public record RAddDataExcel(List<DataList> LsData);
public interface IDriverExcel
{
  void Add(RAddDataExcel list);
  void AddError(int errorNum, string nameStr = "", string comment = "");
  void SaveExcels();
  void SetParams(string path, string nameStrategy);
}
public class DriverExcel : ReactiveObject, IDriverExcel
{
  public ConcurrentQueue<RAddDataExcel> RAddDataExcels = new();
  public ConcurrentQueue<(int, string, string)> ErrorNameComment = new();
  private string _pathLog;
  private string _pathError;
  private string _pathErrorNotMinusError;
  private string _pathErrorMinusError;
  private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  private int _iRow;  //  1,2,3,4,5
  private int _jCol;  //  "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

  public void SetParams(string path, string nameStrategy)
  {
    _pathLog = path;
    //_pathLog = _pathLog + $"\\ExcelData_{nameStrategy}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
    _pathLog += $"\\ExcelData_{nameStrategy}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
    _pathErrorNotMinusError = path + $"\\ExcelError_{nameStrategy}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
    _pathErrorMinusError = path + $"\\~ExcelError_{nameStrategy}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
  }
  public void Add(RAddDataExcel list) => RAddDataExcels.Enqueue(list);
  public void AddError(int errorNum, string nameStr = "", string comment = "") => ErrorNameComment.Enqueue((errorNum, nameStr, comment));

  public void SaveExcels()
  {
    if (RAddDataExcels.IsEmpty && ErrorNameComment.IsEmpty) return;

    // Set EPPlus license - replace with your license method if commercial
    ExcelPackage.License.SetNonCommercialPersonal("Your Name");

    if (!RAddDataExcels.IsEmpty)
    {
      using var package = new ExcelPackage(new FileInfo(_pathLog));
      var worksheet = package.Workbook.Worksheets.Add("data");
      _iRow = 0; //  1,2,3,4,5

      while (!RAddDataExcels.IsEmpty)
      {
        _iRow += 1;
        var isSuccessful = RAddDataExcels.TryDequeue(out var item);

        if (!isSuccessful) continue;

        _jCol = 0; //  "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        foreach (var it in item.LsData)
        {
          var ch = Alphabet[_jCol];
          var cell = ch.ToString() + _iRow.ToString();
          worksheet.Cells[cell].Value = it.TypeData switch
          {
            "string" => it.Data,
            "boolean" => (bool)it.Data,
            "int" => (int)it.Data,
            "double" => (double)it.Data,
            _ => worksheet.Cells[cell].Value
          };

          _jCol++;
        }
      }

      package.SaveAs(new FileInfo(_pathLog));
    }

    if (ErrorNameComment.IsEmpty) return;

    var masQueue = ErrorNameComment.ToArray();
    var indError = masQueue.Count(x => x.Item1 < 0);

    _pathError = indError > 0 ? _pathErrorMinusError : _pathErrorNotMinusError;

    using (var package = new ExcelPackage(new FileInfo(_pathError)))
    {
      var worksheet = package.Workbook.Worksheets.Add("data");
      _iRow = 0;  //  1,2,3,4,5

      while (!ErrorNameComment.IsEmpty)
      {
        _iRow += 1;
        var isSuccessful = ErrorNameComment.TryDequeue(out var item);

        if (!isSuccessful) continue;

        _jCol = 0;  //  "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        worksheet.Cells["A" + _iRow.ToString()].Value = item.Item1;   // Error number
        if (item.Item1 < 0)
        {
          using var range = worksheet.Cells["A" + _iRow.ToString()];
          range.Style.Fill.PatternType = ExcelFillStyle.Solid;
          range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
          range.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }
        worksheet.Cells["B" + _iRow.ToString()].Value = item.Item2;   // Error name
        worksheet.Cells["C" + _iRow.ToString()].Value = item.Item3;   // Comments
      }
      package.SaveAs(new FileInfo(_pathError));
    }
  }
}

///////////////////
// ReSharper disable once InvalidXmlDocComment
///  Примеры!!!!
//////////////////// 
/*
  
  public void SaveExcels()
  {
    if (RAddDataExcels.IsEmpty & ErrorNameComment.IsEmpty) return;

    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    if (!RAddDataExcels.IsEmpty)
    {
      using (var package = new ExcelPackage(new FileInfo("_pathLog")))
      {
        var worksheet = package.Workbook.Worksheets.Add("data");
        _iRow = 0; //  1,2,3,4,5

        while (!RAddDataExcels.IsEmpty)
        {
          _iRow += 1;
          var isSuccessful = RAddDataExcels.TryDequeue(out var item);

          if (!isSuccessful) continue;

          _jCol = 0; //  "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

          foreach (var it in item.LsData)
          {
            var ch = Alphabet[_jCol];
            var cell = ch.ToString() + _iRow.ToString();
            worksheet.Cells[cell].Value = it.TypeData switch
            {
              "string" => it.Data,
              "boolean" => (bool)it.Data,
              "int" => (int)it.Data,
              "double" => (double)it.Data,
              _ => worksheet.Cells[cell].Value
            };

            _jCol++;
          }
        }

        package.SaveAs(_pathLog);
      }
    }

    if (ErrorNameComment.IsEmpty) return;

    var masQueue = ErrorNameComment.ToArray();
    var indError = masQueue.Count(x => x.Item1 < 0);

    _pathError = indError > 0 ? _pathErrorMinusError : _pathErrorNotMinusError;

    using (var package = new ExcelPackage(new FileInfo("_pathError")))
    {
      var worksheet = package.Workbook.Worksheets.Add("data");
      _iRow = 0;  //  1,2,3,4,5

      while (!ErrorNameComment.IsEmpty)
      {
        _iRow += 1;
        var isSuccessful = ErrorNameComment.TryDequeue(out var item);

        if (!isSuccessful) continue;

        _jCol = 0;  //  "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        worksheet.Cells["A" + _iRow.ToString()].Value = item.Item1;   // Номер ошибки
        if (item.Item1 < 0)
        {
          using (var range = worksheet.Cells["A" + _iRow.ToString()])
          {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
          }
        }
        worksheet.Cells["B" + _iRow.ToString()].Value = item.Item2;   // Название ошибки
        worksheet.Cells["C" + _iRow.ToString()].Value = item.Item3;   // Коментарии 
      }
      package.SaveAs(_pathError);
    }
  }

  
 
  
//    Пример поступление данных и печать 

  [Reactive]
  private int _count { get; set; }

//-------------------------------------------------------------------------
  public void Add(RAddDataExcel list)
  {
    raddDataExcels.Enqueue(list);

    Console.WriteLine($"----{raddDataExcels.Count} " );
    _count += 1; 
//      = raddDataExcels.Count;
//    iii += 1;
  }

//---------------
наблюдение 
//    Observable
//      .Interval(TimeSpan.FromSeconds(1))
////      .Where()
//      .Subscribe(time =>
//      {
//        Console.WriteLine(DateTime.Now);
//      });

///-----------------------------------
от события !!!! =====================================

    _count = 0;
    this.WhenAnyValue(x=>x._count)
      .ObserveOn(RxApp.MainThreadScheduler)
      .Subscribe( t=>
      {
        while(!raddDataExcels.IsEmpty)
        {
          bool isSuccessful = raddDataExcels.TryDequeue(out  var item);
          if (isSuccessful)
          {
            Console.WriteLine(" - - - - - - - - - - - - - ");
            foreach (var  it in item.LsData)
              Console.WriteLine($" type => {it.TypeData}  data=>{it.Data} ");
          }
          Console.WriteLine(raddDataExcels.Count);
        }
      }
    );




 */