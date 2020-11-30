using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using WiproServices.Models;

public class Example
{

	public List<(string ID_MOEDA, int COD_COTACAO)> tuples = new List<(string, int)>
			{
				("AFN", 66),
				("ALL", 49 ),
				("ANG", 33),
				("ARS", 3 ),
				("AWG", 6),
				("BOB", 56 ),
				("BYN", 64),
				("CAD", 25 ),
				("CDF", 58),
				("CLP", 16 ),
				("COP", 37),
				("CRC", 52 ),
				("CUP", 8),
				("CVE", 51 ),
				("CZK", 29),
				("DJF", 36 ),
				("DZD", 54),
				("EGP", 12 ),
				("EUR", 20),
				("FJD", 38 ),
				("GBP", 22),
				("GEL", 48 ),
				("GIP", 18),
				("HTG", 63 ),
				("ILS", 40),
				("IRR", 17 ),
				("ISK", 11),
				("JPY", 9 ),
				("KES", 21),
				("KMF", 19 ),
				("LBP", 42),
				("LSL", 4 ),
				("MGA", 35),
				("MGB", 26 ),
				("MMK", 69),
				("MRO", 53 ),
				("MRU", 15),
				("MUR", 7 ),
				("MXN", 41),
				("MZN", 43 ),
				("NIO", 23),
				("NOK", 62 ),
				("OMR", 34),
				("PEN", 45 ),
				("PGK", 2),
				("PHP", 24 ),
				("RON", 5),
				("SAR", 44 ),
				("SBD", 32),
				("SGD", 70 ),
				("SLL", 10),
				("SOS", 61 ),
				("SSP", 47),
				("SZL", 55 ),
				("THB", 39),
				("TRY", 13 ),
				("TTD", 67),
				("UGX", 59 ),
				("USD", 1),
				("UYU", 46 ),
				("VES", 68),
				("VUV", 57 ),
				("WST", 28),
				("XAF", 30 ),
				("XAU", 60),
				("XDR", 27 ),
				("XOF", 14),
				("XPF", 50 ),
				("ZAR", 65),
				("ZWL", 31)
			};

	private static void Main()
	{
		while (Start())
		{
			//Thread.Sleep(120000);
			Thread.Sleep(10000);
		}
	}

	private static bool Start()
	{
		Stopwatch sw = new Stopwatch();

		sw.Start();

		Console.WriteLine("Process Started: " + DateTime.Now);

		GetItem();

		Console.WriteLine("Process Finished: " + DateTime.Now);

		sw.Stop();

		Console.WriteLine("Processing Time {0} ms", sw.ElapsedMilliseconds);

		GC.Collect();

		return true;
	}

	private static void GetItem()
	{
		var url = $"https://localhost:44397/api/ProcessingQueue";
		var request = (HttpWebRequest)WebRequest.Create(url);
		request.Method = "GET";
		request.ContentType = "application/json";
		request.Accept = "application/json";

		try
		{
			WebResponse response = request.GetResponse();
			Stream strReader = response.GetResponseStream();
			if (strReader == null) return;
			StreamReader objReader = new StreamReader(strReader);
			string responseBody = objReader.ReadToEnd();
			objReader.Close();
			response.Close();

			CompareFiles(JsonConvert.DeserializeObject<DadosConsultado>(responseBody));
		}
		catch (WebException ex)
		{
			Console.WriteLine("Error: " + DateTime.Now + " - " + ex.Message.ToString());
		}
	}

	private static void CompareFiles(DadosConsultado listDadosConsultado)
	{
		try
		{
			List<DadosMoeda> listDadosMoeda = ReadFIleDadosMoeda();
			listDadosMoeda.RemoveAll(x => x.DATA_REF == "DATA_REF");

			List<DadosCotacao> listDadosCotacao = ReadFileDadosCotacao();
			listDadosCotacao.RemoveAll(x => x.COD_COTACAO == "cod_cotacao");

			List<Depara> listDepara = ReadFileDepara();
			listDepara.RemoveAll(x => x.COD_COTACAO == "COD_COTACAO");

			List<DadosMoeda> listMoedasDatas = (from l in listDadosMoeda
												where Convert.ToDateTime(l.DATA_REF) >= Convert.ToDateTime(listDadosConsultado.DATA_INICIO) &&
													  Convert.ToDateTime(l.DATA_REF) <= Convert.ToDateTime(listDadosConsultado.DATA_FIM)
												select l).ToList();

			//1.2. Com a lista de moedas/datas, buscar todos os valores de cotação (vlr_cotacao) no arquivo DadosCotacao.csv utilizando o de-para descrito no item 4 (Tabela de de-para) para obter as cotações.

			//var result = from e in ReadFileDepara()
			//			 join d in ReadFIleDadosMoeda() on e.ID_MOEDA equals d.ID_MOEDA
			//			 join c in ReadFileDadosCotacao() on e.COD_COTACAO equals c.COD_COTACAO
			//			 into eGroup
			//			 from c in eGroup.DefaultIfEmpty()
			//			 select new
			//			 {
			//				 DATA_REF = d.DATA_REF,
			//				 ID_MOEDA = d.ID_MOEDA,
			//				 VLR_COTACAO = c.VLR_COTACAO
			//			 };


			WriteFileCSV(listMoedasDatas);
		}
		catch (WebException ex)
		{
			Console.WriteLine("Error: " + DateTime.Now + " - " + ex.Message.ToString());
		}
	}

	private static List<DadosMoeda> ReadFIleDadosMoeda()
	{
		try
		{
			string pathDadosMoeda = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\DadosMoeda.csv"));

			var listDadosMoeda = File.ReadAllLines(pathDadosMoeda)
					.Select(a => a.Split(';'))
					.Select(c => new DadosMoeda()
					{
						ID_MOEDA = c[0],
						DATA_REF = c[1]
					})
					.ToList();

			return listDadosMoeda;
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}

	private static List<DadosCotacao> ReadFileDadosCotacao()
	{

		try
		{
			string pathDadosCotacao = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\DadosCotacao.csv"));

			var listDadosCotacao = File.ReadAllLines(pathDadosCotacao)
					.Select(a => a.Split(';'))
					.Select(c => new DadosCotacao()
					{
						VLR_COTACAO = c[0],
						COD_COTACAO = c[1],
						DAT_COTACAO = c[2]
					})
					.ToList();
			return listDadosCotacao;
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}

	private static List<Depara> ReadFileDepara()
	{
		try
		{
			string pathDepara = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Depara.csv"));

			var listDepara = File.ReadAllLines(pathDepara)
					.Select(a => a.Split(';'))
					.Select(c => new Depara()
					{
						ID_MOEDA = c[0],
						COD_COTACAO = c[1]
					})
					.ToList();

			return listDepara;
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}

	private static void WriteFileCSV(IEnumerable<DadosMoeda> listMoedasDatas)
	{
		string dateTime = DateTime.Now.ToString();
		string dateFormatted = dateTime.Substring(6, 4) + dateTime.Substring(3, 2) + dateTime.Substring(0, 2);
		string timeFormatted = dateTime.Substring(11, 8).Replace(":", "");
		string name = $"Resultado_{dateFormatted}_{timeFormatted}.csv";
		string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

		try
		{
			var allLines = (from row in listMoedasDatas
							select new object[]
							{
								row.DATA_REF.ToString(),
								row.ID_MOEDA.ToString(),
							}
				).ToList();

			var csv = new StringBuilder();

			allLines.ForEach(line =>
			{
				csv.AppendLine(string.Join(";", line));
			});

			File.WriteAllText($"{path}\\{name}", csv.ToString());
		}
		catch (WebException ex)
		{
			Console.WriteLine("Error: " + DateTime.Now + " - " + ex.Message.ToString());
		}
	}
}
