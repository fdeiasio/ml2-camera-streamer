using System;
using System.Collections.Generic;
using System.IO;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public class MagicLeapSdkVersionComparer: IComparer<string>
    {

        			private string GetVersionString(string inputString)
        			{
        				string returnVal = inputString;
        				if (string.IsNullOrEmpty(inputString))
        				{
        					return "0.0.0_dev0-ec0";
        				}
        
        
        				if (Directory.Exists(inputString))
        				{
        					returnVal = new DirectoryInfo(inputString).Name;
        				}
        
        				if (File.Exists(inputString))
        				{
        					returnVal = new FileInfo(inputString).Name;
        				}
        
        				returnVal= returnVal.Replace("\\", "/");
        				var lastIndexOfSlash = returnVal.IndexOf('/');
        				if (lastIndexOfSlash > -1)
        				{
        					returnVal = returnVal.Substring(lastIndexOfSlash + 1, returnVal.Length - (lastIndexOfSlash + 1));
        				}
        
        				var indexOfV = returnVal.IndexOf('v');
        				if (indexOfV > -1)
        				{
        					returnVal = returnVal.Substring(indexOfV + 1, returnVal.Length - (indexOfV + 1));
        				}
        
        
        
        				return returnVal;
        			}
        
        		
        			public int Compare(string x, string y)
        			{
        				if (string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y))
        				{
        					return 0;
        				}
        
        				x = GetVersionString(x);
        				y = GetVersionString(y);
        				var xParts = x.Split('-', '_', '.');
        				var yParts = y.Split('-', '_', '.');
        
        				var length = Math.Max(xParts.Length, yParts.Length);
        
        				for (int i = 0; i < length; i++)
        				{
        					if (i >= xParts.Length)
        						return 1;
        					if (i >= yParts.Length)
        						return -1;
        
        					if (int.TryParse(xParts[i], out int xNum) && int.TryParse(yParts[i], out int yNum))
        					{
        						if (xNum != yNum)
        							return xNum.CompareTo(yNum);
        					}
        					else
        					{
        						int compareResult = string.Compare(xParts[i], yParts[i], StringComparison.Ordinal);
        						if (compareResult != 0)
        							return compareResult;
        					}
        				}
        
        				return 0;
        			}
        		}
        	
    
}