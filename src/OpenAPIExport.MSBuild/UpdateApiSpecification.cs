/*
	Copyright (c) 2022-2024 Y56380X
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
	public ITaskItem? ExportPath { get; set; }
	public ITaskItem? ServerPort { get; set; }
	public ITaskItem? ExportSource { get; set; }
	public ITaskItem? RetryCount { get; set; }
	public ITaskItem? RetryInterval { get; set; }
	
	public override bool Execute()
	{
		Log.LogMessage(MessageImportance.High, $"Running {nameof(UpdateApiSpecification)}");

		// Set variables
		var path = ExportPath is {} exportPath
			? exportPath.ToString()!
			: Path.Combine(SolutionDirectory.ToString()!, "doc", "api-doc.yaml");
		Log.LogMessage(MessageImportance.Low, $"Path: {path}");
		var port = ServerPort is {} exportPort
			? int.Parse(exportPort.ToString()!)
			: 5005;
		Log.LogMessage(MessageImportance.Low, $"Port: {port}");
		var source = ExportSource is { } exportSource
			? exportSource.ToString()!
			: "swagger/v1/swagger.yaml";
		Log.LogMessage(MessageImportance.Low, $"Source: {source}");
		var retryLimit = RetryCount is {} retryCount
			? int.Parse(retryCount.ToString()!)
			: 5;
		var retryIntervalSeconds = RetryInterval is { } retryInterval
			? int.Parse(retryInterval.ToString()!)
			: 2;
		
		// Start api application
		var targetAssembly = new FileInfo(TargetAssemblyPath.ToString()!);
		Log.LogMessage(MessageImportance.Low, $"Assembly: {targetAssembly}");
		var apiStartInfo = new ProcessStartInfo("dotnet")
		{
			ArgumentList =
			{
				targetAssembly.FullName,
				$"--urls=http://localhost:{port}/"
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
		httpClient.BaseAddress = new Uri($"http://localhost:{port}/");
		HttpResponseMessage? response;
		do
		{
			Thread.Sleep(TimeSpan.FromSeconds(retryIntervalSeconds));
			try
			{
				response = httpClient.GetAsync(source).Result;
				response.EnsureSuccessStatusCode();
			}
			catch
			{
				response = null;
				tries++;
			}
		} while (tries < retryLimit && !(response?.IsSuccessStatusCode ?? false));

		// Stop api application
		apiProcess.Kill();
		apiProcess.WaitForExit();
		
		// If download successful, update doc file
		if (response is { IsSuccessStatusCode: true })
		{
			File.WriteAllBytes(path, response.Content.ReadAsByteArrayAsync().Result);
			Log.LogMessage(MessageImportance.High, "Updating API Specification successful");
		}
		
		return response is { IsSuccessStatusCode: true };
	}
}
