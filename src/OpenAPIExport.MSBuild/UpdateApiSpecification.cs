using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace OpenAPIExport.MSBuild;

public class UpdateApiSpecification : Task
{
	public override bool Execute()
	{
		Log.LogMessage(MessageImportance.High, $"Running {nameof(UpdateApiSpecification)}");
		return true;
	}
}
