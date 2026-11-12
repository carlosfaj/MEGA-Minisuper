using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Mega.Web.Services;
public static class ReportExport
{
    static ReportExport()=>GlobalFontSettings.FontResolver=new ReportFontResolver();
    public static byte[] Excel(ReportSnapshot report)
    {
        using var book=new XLWorkbook();book.Properties.Title=report.Title;book.Properties.Author="MEGA";
        var summary=book.AddWorksheet("Resumen");summary.Style.Font.FontName="Noto Sans";summary.Style.Font.FontSize=11;
        summary.Columns(1,5).Width=23;summary.Cell(1,1).Value="MEGA · "+report.Title;summary.Range(1,1,1,5).Merge().Style.Font.SetFontSize(18).Font.SetBold();
        summary.Row(1).Height=30;summary.Style.Alignment.Vertical=XLAlignmentVerticalValues.Center;
        summary.Cell(2,1).Value=report.Period;summary.Range(2,1,2,5).Merge();summary.Cell(3,1).Value=report.Scope;summary.Range(3,1,3,5).Merge();summary.Cell(4,1).Value="Generado: "+Formats.DateWithSeconds(report.GeneratedAt);summary.Range(4,1,4,5).Merge();
        summary.Cell(5,1).Value=report.Note;summary.Range(5,1,5,5).Merge().Style.Alignment.SetWrapText();summary.Row(5).Height=85;
        var metricRow=7;foreach(var metric in report.Metrics){summary.Cell(metricRow,1).Value=metric.Label;summary.Range(metricRow,1,metricRow,3).Merge();summary.Cell(metricRow,4).Value=metric.Value;summary.Range(metricRow,4,metricRow,5).Merge();summary.Row(metricRow).Height=25;metricRow++;}
        summary.Range(7,4,metricRow-1,5).Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;summary.SheetView.FreezeRows(6);
        foreach(var table in report.Tables)
        {
            var sheet=book.AddWorksheet(table.Title[..Math.Min(31,table.Title.Length)]);var columns=table.Headers.Length;sheet.Style.Font.FontName="Noto Sans";sheet.Style.Font.FontSize=10;sheet.Columns(1,columns).Width=23;
            sheet.Cell(1,1).Value=table.Title;sheet.Range(1,1,1,columns).Merge().Style.Font.SetFontSize(16).Font.SetBold();sheet.Cell(2,1).Value=report.Period;sheet.Range(2,1,2,columns).Merge();sheet.Cell(3,1).Value=report.Scope;sheet.Range(3,1,3,columns).Merge();sheet.Cell(4,1).Value="Moneda NIO · generado "+Formats.DateWithSeconds(report.GeneratedAt);sheet.Range(4,1,4,columns).Merge();
            sheet.Row(1).Height=28;sheet.Style.Alignment.Vertical=XLAlignmentVerticalValues.Center;
            for(var col=0;col<columns;col++)sheet.Cell(6,col+1).Value=table.Headers[col];
            sheet.Range(6,1,6,columns).Style.Fill.BackgroundColor=XLColor.FromHtml("#DDEBF1");sheet.Range(6,1,6,columns).Style.Font.FontColor=XLColor.FromHtml("#163A4D");sheet.Range(6,1,6,columns).Style.Font.Bold=true;sheet.Row(6).Height=30;
            for(var row=0;row<table.Rows.Count;row++)for(var col=0;col<columns;col++)
            {
                var value=table.Rows[row][col];var cell=sheet.Cell(row+7,col+1);
                if(value.Number is decimal number&&Math.Abs(number)<100000000000000m)
                {cell.Value=(double)number;cell.Style.NumberFormat.Format=table.Headers[col] is "Cantidad" or "Existencia" or "Mínimo"?(number%1==0?"0":"0.###"):table.Headers[col].StartsWith("Costo unitario")?(number%1==0?"0.00":"0.######"):"#,##0.00";}
                else cell.Value=value.Text;
                cell.Style.Alignment.WrapText=true;
            }
            if(table.Rows.Count>0)sheet.Range(6,1,table.Rows.Count+6,columns).SetAutoFilter();sheet.SheetView.FreezeRows(6);
            for(var row=0;row<table.Rows.Count;row++)sheet.Row(row+7).Height=Math.Min(409,Math.Max(30,Math.Ceiling(table.Rows[row].Max(x=>x.Text.Length)/22d)*14+8));
        }
        using var stream=new MemoryStream();book.SaveAs(stream);return stream.ToArray();
    }
    public static byte[] Pdf(ReportSnapshot report)
    {
        using var doc=new PdfDocument();doc.Info.Title="MEGA · "+report.Title;doc.Info.Author="MEGA";
        var normal=new XFont("Noto Sans",8);var bold=new XFont("Noto Sans",8,XFontStyleEx.Bold);var title=new XFont("Noto Sans",18,XFontStyleEx.Bold);
        XGraphics? gfx=null;PdfPage page=null!;double y=0;var pageNo=0;
        void NewPage()
        {
            gfx?.Dispose();page=doc.AddPage();page.Size=PdfSharp.PageSize.A4;page.Orientation=PdfSharp.PageOrientation.Landscape;gfx=XGraphics.FromPdfPage(page);pageNo++;
            gfx.DrawString("MEGA · "+report.Title,title,XBrushes.DarkSlateGray,new XPoint(35,35));gfx.DrawString(report.Period.Replace('−','-'),normal,XBrushes.Black,new XPoint(35,53));gfx.DrawString(report.Scope,normal,XBrushes.Black,new XPoint(35,67));
            gfx.DrawString($"Generado {Formats.DateWithSeconds(report.GeneratedAt)} · NIO · página {pageNo}",normal,XBrushes.Gray,new XPoint(35,page.Height.Point-20));y=85;
        }
        NewPage();
        foreach(var line in Wrap(gfx!,report.Note,normal,page.Width.Point-70)){gfx!.DrawString(line,normal,XBrushes.Gray,new XPoint(35,y));y+=12;}y+=12;
        foreach(var metric in report.Metrics){if(y>page.Height.Point-50)NewPage();gfx!.DrawString(metric.Label+": "+metric.Value,bold,XBrushes.Black,new XPoint(35,y));y+=16;}y+=15;
        foreach(var table in report.Tables)
        {
            var width=(page.Width.Point-70)/table.Headers.Length;
            void Header()
            {
                gfx!.DrawString(table.Title,new XFont("Noto Sans",12,XFontStyleEx.Bold),XBrushes.DarkSlateGray,new XPoint(35,y));y+=12;
                var wraps=table.Headers.Select(x=>Wrap(gfx!,x,bold,width-10)).ToArray();var height=wraps.Max(x=>x.Count)*12+12;
                gfx!.DrawRectangle(XBrushes.DarkSlateGray,35,y,page.Width.Point-70,height);
                for(var c=0;c<wraps.Length;c++)for(var l=0;l<wraps[c].Count;l++)gfx.DrawString(wraps[c][l],bold,XBrushes.White,new XPoint(40+c*width,y+13+l*12));y+=height;
            }
            if(y>page.Height.Point-110)NewPage();Header();
            for(var row=0;row<table.Rows.Count;row++)
            {
                var cells=table.Rows[row].Select(x=>Wrap(gfx!,x.Text,normal,width-10)).ToArray();var height=cells.Max(x=>x.Count)*12+10;
                if(y+height>page.Height.Point-40){NewPage();Header();}
                if(row%2==0)gfx!.DrawRectangle(XBrushes.AliceBlue,35,y,page.Width.Point-70,height);
                for(var c=0;c<cells.Length;c++)for(var l=0;l<cells[c].Count;l++)gfx!.DrawString(cells[c][l],normal,XBrushes.Black,new XPoint(40+c*width,y+13+l*12));y+=height;
            }
            if(table.Rows.Count==0){gfx!.DrawString("Sin registros en este alcance.",normal,XBrushes.Gray,new XPoint(35,y+15));y+=25;}y+=30;
        }
        gfx?.Dispose();using var stream=new MemoryStream();doc.Save(stream,false);return stream.ToArray();
    }
    private static List<string> Wrap(XGraphics gfx,string value,XFont font,double width)
    {
        var result=new List<string>();var line="";
        foreach(var c in value.Replace('−','-').Select(c=>char.IsControl(c)?' ':c))
        {
            if(gfx.MeasureString(line+c,font).Width>width&&line.Length>0)
            {var space=line.LastIndexOf(' ');if(space>0){result.Add(line[..space]);line=line[(space+1)..];}else{result.Add(line);line="";}}
            line+=c;
        }
        result.Add(line);return result;
    }
    private sealed class ReportFontResolver:IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName,bool bold,bool italic)=>familyName=="Noto Sans"?new("NotoSans-Regular",bold,italic):null;
        public byte[] GetFont(string faceName)
        {using var font=typeof(ReportExport).Assembly.GetManifestResourceStream("Mega.Web.Assets.Fonts.NotoSans-Regular.ttf")??throw new InvalidOperationException("Falta la fuente incluida en la distribución.");using var stream=new MemoryStream();font.CopyTo(stream);return stream.ToArray();}
    }
}
