Add-PSSnapin citrix*
New-PSDrive -name cgp -PSProvider CitrixGroupPolicy -Root "" -Controller localhost
$GPTreePath=".\GPTree.xml"

Function Generate-PolicyTree
{
	$GP=@{}
    $GPUserTree=@(get-ChildItem -path cgp:\user\Unfiltered\settings -recurse | ?{$_.psobject.Properties.name.Contains("State")}|Select PSPath)
	$GP.Add("User",$GPUserTree)
    $GPComputerTree= @(get-ChildItem -path cgp:\computer\Unfiltered\settings -recurse | ?{$_.psobject.Properties.name.Contains("State")}|Select PSPath)
	$GP.Add("Computer",$GPComputerTree)
    $GP| export-clixml $GPTreePath
}

Function Get-GPHsTable
{ 
    if(-not (Test-Path $GPTreePath))
    {
        throw "do not find the GPTree file"
    }
    $GPAllTree=import-clixml $GPTreePath
    $GPNames=Get-GPNames
    $ReturnPolicyArray=@()
    foreach($eachName in $GPNames)
    {
		if(($eachName.Context -eq "All") -or ($eachName.Context -eq "User"))
		{
			$GPTree=$GPAllTree["User"]
			foreach($eachGPTreeNode in $GPTree)
			{
				$path = $eachGPTreeNode.PSPath.Remove(0,45).replace("Unfiltered",$eachName.Name)
				$eachGP= Get-Item -path $eachGPTreeNode.PSPath.Remove(0,45)
				$ConfGPItem = Get-Item -path $path
				$UserContext = "User"
				if($ConfGPItem.State -ne $eachGP.State)
				{
					$GPHt=@{GPName=[string]($eachName.name);UserContext=$UserContext;PolicyName=[string]($eachGP.PSChildName);State="";Value="";Values=@()}
					if(($ConfGPItem.psobject.Properties.name.Contains("Value")) -and ($ConfGPItem.value -ne ""))
					{
						$GPHt.Value=[string]($ConfGPItem.value)
					}
					elseif(($ConfGPItem.psobject.Properties.name.Contains("Values")) -and ($ConfGPItem.values[0] -ne ""))
					{
						$Values=@()
						$GPHt.Values=[string[]]$ConfGPItem.values
					}
					else 
					{
						$GPHt.State=[string]($ConfGPItem.State)
					}
					$ReturnPolicyArray+=$GPHt
				}
			}
		}
		if(($eachName.Context -eq "All") -or ($eachName.Context -eq "Computer"))
		{
			$GPTree=$GPAllTree["Computer"]
			foreach($eachGPTreeNode in $GPTree)
			{
				$path = $eachGPTreeNode.PSPath.Remove(0,45).replace("Unfiltered",$eachName.Name)
				$eachGP= Get-Item -path $eachGPTreeNode.PSPath.Remove(0,45)
				$ConfGPItem = Get-Item -path $path
				$UserContext = "Computer"
				if($ConfGPItem.State -ne $eachGP.State)
				{
					$GPHt=@{GPName=[string]($eachName.name);UserContext=$UserContext;PolicyName=[string]($eachGP.PSChildName);State="";Value="";Values=@()}
					if(($ConfGPItem.psobject.Properties.name.Contains("Value")) -and ($ConfGPItem.value -ne ""))
					{
						$GPHt.Value=[string]($ConfGPItem.value)
					}
					elseif(($ConfGPItem.psobject.Properties.name.Contains("Values")) -and ($ConfGPItem.values[0] -ne ""))
					{
						$Values=@()
						$GPHt.Values=[string[]]$ConfGPItem.values
					}
					else 
					{
						$GPHt.State=[string]($ConfGPItem.State)
					}
					$ReturnPolicyArray+=$GPHt
				}
			}
			
		}
    }
    return $ReturnPolicyArray 

}

Function Get-GPNames
{
    $GPItems = @(get-ChildItem -path cgp:\user | ?{$_.name -ne "Unfiltered"})
    $returnArray = @()
    $GPItems | %{
        $tmp=@{Name=$_.Name;Priority=$_.Priority;Description=$_.Description;Context="User"}
        $returnArray+=$tmp
    }
    $GPItems = @(get-ChildItem -path cgp:\computer | ?{$_.name -ne "Unfiltered" })
	if($returnArray.Count -eq 0)
	{		
		$GPItems | %{
			$tmp=@{Name=$_.Name;Priority=$_.Priority;Description=$_.Description;Context="Computer"}
			$returnArray+=$tmp
		}
	}
	else
	{
		$GPItems | %{
			$AllContextFlag=$false
			foreach($each in $returnArray)
			{
				if($each.Name -eq $_.Name)
				{
					$AllContextFlag=$true
					$each.Context="All"
					break
				}
			}
			if(!$AllContextFlag)
			{
				$tmp=@{Name=$_.Name;Priority=$_.Priority;Description=$_.Description;Context="Computer"}
				$returnArray += $tmp
			}
		}
	}
    return $returnArray 
}

$GpPolicy = Get-GPHsTable 
return $GpPolicy
