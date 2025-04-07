using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextFSM
{
    /// <summary>
    /// Helper class specifically for PowerShell integration
    /// </summary>
    public static class PowerShellHelper
    {
        /// <summary>
        /// Parse text using a TextFSM template and return results as a JSON string
        /// </summary>
        public static string ParseTextToJson(string templateText, string inputText)
        {
            try
            {
                // Create a TextFSM instance
                var fsm = new TextFSM(templateText);
                
                // Parse the text
                var resultLists = fsm.ParseText(inputText);
                var headers = fsm.Header;
                
                // Create a list of dictionaries
                var resultObjects = new List<Dictionary<string, object>>();
                
                foreach (var row in resultLists)
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < headers.Count && i < row.Count; i++)
                    {
                        dict[headers[i]] = row[i];
                    }
                    resultObjects.Add(dict);
                }
                
                // Serialize the results
                return JsonConvert.SerializeObject(resultObjects, Formatting.Indented);
            }
            catch (Exception ex)
            {
                // Return error as JSON
                var error = new Dictionary<string, string>();
                error["Error"] = ex.Message;
                error["StackTrace"] = ex.StackTrace;
                return JsonConvert.SerializeObject(error, Formatting.Indented);
            }
        }

        /// <summary>
        /// Parse text using a TextFSM template and return results as a JSON string with optional pretty printing
        /// </summary>
        public static string ParseTextToJson(string templateText, string inputText, bool prettyPrint)
        {
            try
            {
                // Create a TextFSM instance
                var fsm = new TextFSM(templateText);
                
                // Parse the text
                var resultLists = fsm.ParseText(inputText);
                var headers = fsm.Header;
                
                // Create a list of dictionaries
                var resultObjects = new List<Dictionary<string, object>>();
                
                foreach (var row in resultLists)
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < headers.Count && i < row.Count; i++)
                    {
                        dict[headers[i]] = row[i];
                    }
                    resultObjects.Add(dict);
                }
                
                // Serialize the results with formatting based on prettyPrint parameter
                Formatting formatting = prettyPrint ? Formatting.Indented : Formatting.None;
                return JsonConvert.SerializeObject(resultObjects, formatting);
            }
            catch (Exception ex)
            {
                // Return error as JSON
                var error = new Dictionary<string, string>();
                error["Error"] = ex.Message;
                error["StackTrace"] = ex.StackTrace;
                
                // Use the requested formatting
                Formatting formatting = prettyPrint ? Formatting.Indented : Formatting.None;
                return JsonConvert.SerializeObject(error, formatting);
            }
        }
    }
}