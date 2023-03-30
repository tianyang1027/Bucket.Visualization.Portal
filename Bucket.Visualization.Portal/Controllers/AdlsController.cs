using System;
using Microsoft.Azure.DataLake.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using VcClient.Test;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Management.CosmosDB.Models;
using VcClient;
using System.Reflection;
using Microsoft.Azure.Management.DataFactory.Models;
using System.Diagnostics;
using Bucket.Visualization.Portal.Models;

namespace Bucket.Visualization.Portal.Controllers
{
    [ApiController]
    public class AdlsController
    {
        private readonly CosmosFileProvider _cosmosFileProvider;

        public AdlsController(CosmosFileProvider cosmosFileProvider)
        {
            if (_cosmosFileProvider == null)
            {
                _cosmosFileProvider = cosmosFileProvider;
            }
        }

        [Route("/Adls/GetPath")]
        public JsonResult GetPath(string filePath)
        {
            //string cosmosPath = filePath.Substring(0, filePath.IndexOf('?'));

            //string buketVisualStatsPath = $"{cosmosPath}/_buket_visual_stats.tsv";
            //string buketVisualSamplesPath = $"{cosmosPath}/_buket_visual_samples.tsv";

            string buketVisualStatsPath = "https://cosmos11.osdinfra.net/cosmos/MMRepository.prod/local/users/v-yangtian/BucketVisualizationPortal/stats.tsv";
            string buketVisualSamplesPath = "https://cosmos11.osdinfra.net/cosmos/MMRepository.prod/local/users/v-yangtian/BucketVisualizationPortal/samples.tsv";

            var buketVisualStatsLocationr = new CosmosResourceLocator(buketVisualStatsPath, DateTime.Now);
            var buketVisualSamplesLocationr = new CosmosResourceLocator(buketVisualSamplesPath, DateTime.Now);
            var statsStram = _cosmosFileProvider.ReadStream(buketVisualStatsLocationr);
            var statsList = ReadSreamToList<StatsViewModel>(statsStram);
            var samplesStram = _cosmosFileProvider.ReadStream(buketVisualSamplesLocationr);
            var samplesList = ReadSreamToList<SamplesViewModel>(samplesStram);
            List<VisViewModel> visViewModels = new List<VisViewModel>();
            //statsList.ForEach(x =>
            //{
            //    visViewModels.Add(new VisViewModel
            //    {
            //        Stats = x,
            //        Samples = samplesList.FindAll(t => x.BucketName == x.BucketName)
            //    });
            //});


            foreach (var item in statsList)
            {
                var model = new VisViewModel
                {
                    Stats = item,
                    Samples = samplesList.FindAll(t => item.BucketName == item.BucketName)
                };
                visViewModels.Add(model);
            }
            return new JsonResult(visViewModels);


        }

        private List<T> ReadSreamToList<T>(Stream stream) where T : class
        {
            StreamReader sr = new StreamReader(stream);
            string? line = string.Empty;
            List<T> list = new List<T>();
            int index = 0;
            string[] headers = { };
            while ((line = sr.ReadLine()) != null)
            {
                if (index == 0)
                {
                    headers = line.Split('\t');
                }
                else
                {
                    T t = Activator.CreateInstance<T>();
                    var cells = line.Split('\t');
                    for (int i = 0; i < cells.Length; i++)
                    {
                        string name = headers[i];
                        var property = t.GetType().GetProperty(name);
                        object value = cells[i];
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(t, value, null);
                        }
                    }
                    list.Add(t);
                }
                index++;
            }
            return list;
        }

        private static ActionResult ProcessError(int statusCode, string message) => new ContentResult
        {
            StatusCode = statusCode,
            Content = message,
            ContentType = "text/plain",
        };
    }
}