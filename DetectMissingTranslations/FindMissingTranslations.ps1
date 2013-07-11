param (
	$resourceFileName = $(throw "must specify a resource file name")
)

function Get-ScriptDirectory {
	$Invocation = (Get-Variable MyInvocation -Scope 1).Value
	Write-Output $(Split-Path $Invocation.MyCommand.Path)
}

$dir = Get-ScriptDirectory
[xml] $baseResourceFile = Get-Content -Path $(Join-Path $dir $($resourceFileName + ".resx"))
$baseStrings = @{}
$otherResourceFiles = gci $(Join-Path $dir $($resourceFileName + "*.resx"))
$numMissingTranslations = 0

$baseResourceFile.root.data | %{
	$baseStrings.Add($_.name, "")
}

$otherResourceFiles | % {
	[xml]$file = Get-Content -Path $_
	$fileName = $_.Name
	$otherStrings = @{}
	$file.root.data | %{
		$otherStrings.Add($_.name, "")
	}
	
	$baseStrings.Keys | % {
		if(-not $otherStrings.ContainsKey($_)) {
			echo "$fileName does not contain a translation for '$_' " 
			$numMissingTranslations++
		}
	}
}

if ($numMissingTranslations -ne 0) {
	Write-Host "$numMissingTranslations missing translations found...Time to reach out to Language Line!"
	exit 1
}