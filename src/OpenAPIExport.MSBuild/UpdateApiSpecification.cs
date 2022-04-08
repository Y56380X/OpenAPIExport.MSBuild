/*
	Copyright (c) 2022 Y56380X
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace OpenAPIExport.MSBuild;

public class UpdateApiSpecification : Task
{
	public ITaskItem TargetAssemblyPath { get; set; }
	
	public ITaskItem SolutionDirectory { get; set; }
	
	public override bool Execute()
	{
		Log.LogMessage(MessageImportance.High, $"Running {nameof(UpdateApiSpecification)}");

		// Start api
		var targetAssembly = new FileInfo(TargetAssemblyPath.ToString()!);
		var apiStartInfo = new ProcessStartInfo("dotnet")
		{
			ArgumentList =
			{
				targetAssembly.FullName,
				"--urls=http://localhost:5005/"
			},
			EnvironmentVariables =
			{
				{"ASPNETCORE_ENVIRONMENT", "Development"}
			},
			WorkingDirectory = targetAssembly.DirectoryName!
		};
		using var apiProcess = Process.Start(apiStartInfo)!;
		
		// Download swagger doc
		var tries = 0;
		using var httpClient = new HttpClient();
		HttpResponseMessage? response;
		do
		{
			Thread.Sleep(TimeSpan.FromSeconds(2));
			try
			{
				response = httpClient.GetAsync("http://localhost:5005/swagger/v1/swagger.yaml").Result;
				response.EnsureSuccessStatusCode();
			}
			catch
			{
				response = null;
				tries++;
			}
		} while (tries < 3 && !(response?.IsSuccessStatusCode ?? false));

		// If download successful, update doc file
		if (response != null)
		{
			File.WriteAllBytes(Path.Combine(SolutionDirectory.ToString()!, "doc", "api-doc.yaml"),
				response.Content.ReadAsByteArrayAsync().Result);
			Log.LogMessage(MessageImportance.High, "Updating API Specification successful");
		}
		
		// Clean up
		apiProcess.Kill();
		apiProcess.WaitForExit();
		
		return response != null;
	}
}
