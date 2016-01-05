using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

namespace UIAutomationHelperDemo.GenerateCode
{
    public class ASF
    {
        private const string Pattern1=@"
        <TestDefinition>
            <Tag Name=""Action"" Value=""Description"" />
            <TestDescription>Description</TestDescription>
            <TimeLimit>00:05:00</TimeLimit>
            <Function Name=""";

        private const string Pattern2=@"""
                      Args=""";

        private const string Pattern3=@"""
                      ModuleName=""UIAutomation""
                      ModuleBasePath=""Community"" />
            <ExecuteOn>DDC</ExecuteOn>
            <UserContext>Administrator</UserContext>
        </TestDefinition>
";
        private const string GPPatternEnd = @"""
                      ModuleName=""GroupPolicy""
                      ModuleBasePath=""TestAPI"" />
            <ExecuteOn>DDC</ExecuteOn>
            <UserContext>Administrator</UserContext>
        </TestDefinition>
";
        

        private static ASF _instance;

        public static ASF Instance
        {
            get {
                if (_instance == null)
                {
                    _instance = new ASF();
                }
                return _instance;
            }
        }

        public string GetButtonTD(AutomationElement UIElem, string ContainerName)
        {
            string FuncName = "Invoke-UIElementMethodByNameAndType_DLL";
            string FuncArgs="";
            if (UIElem.Current.Name != "")
            {
                FuncArgs = string.Format("-Type Button -FunctionName Invoke -ContainerName '{0}' -Name '{1}' -PatternName InvokePattern -Verbose -timeout 120", ContainerName, UIElem.Current.Name);
            }
            string codeStr = Pattern1 + FuncName + Pattern2 + FuncArgs + Pattern3;
            return codeStr;
        }

        public string GetCTXGPConfigTD(List<GPLib.GPPolicyEnty> gpPolicyConfig)
        {
            string codeHead="";
            string codeStr="";
            string codeTail = "";
            string FuncName = "Set-CtxGPConfiguration";
            List<string> gpNames = new List<string>();
            bool ComputerContext = false;
            foreach (GPLib.GPPolicyEnty each in gpPolicyConfig)
            {
                if (each.UserContext == "Computer" && ComputerContext == false)
                {
                    ComputerContext = true;
                }
                if (!gpNames.Contains(each.GpName))
                {
                    gpNames.Add(each.GpName);
                }
                string FuncArgs="";
                if (!string.IsNullOrEmpty(each.Value))
                {
                    FuncArgs = string.Format("-Name {0} -GPName {1}  -Context {2} -Value '{3}' -Verbose", each.PolicyName, each.GpName, each.UserContext, each.Value);
                }
                if (!string.IsNullOrEmpty(each.State))
                {
                    FuncArgs = string.Format("-Name {0} -GPName {1}  -Context {2} -State {3} -Verbose", each.PolicyName, each.GpName, each.UserContext, each.State);
                }
                if (each.Values!=null)
                {
                    StringBuilder sb = new StringBuilder(1024);
                    FuncArgs = string.Format("-Name {0} -GPName {1}  -Context {2} -Values @(", each.PolicyName, each.GpName, each.UserContext);
                    foreach (string eachValue in each.Values)
                    {
                        FuncArgs += "'" + eachValue + "', ";
                    }
                    FuncArgs=FuncArgs.Trim().TrimEnd(',');
                    FuncArgs += ") -Verbose";
                }
                codeStr += Pattern1 + FuncName + Pattern2 + FuncArgs + GPPatternEnd;
            }
            codeHead = GetCTXTD(gpNames);
            codeTail = GetGPTail(ComputerContext);
            codeStr = codeHead + codeStr + codeTail;
            return codeStr;
        }

        public string GetCTXTD(List<string> gpPolicyNames)
        {
            string codeStr="";
            string FuncNameRemove = "Remove-CtxGroupPolicy";
            string FuncNameAdd = "New-CtxGroupPolicy";
            foreach (string each in gpPolicyNames)
            {
                string FuncArgs = string.Format("-Name {0} -Context All -Verbose",each);
                codeStr += Pattern1 + FuncNameRemove + Pattern2 + FuncArgs + GPPatternEnd;
                codeStr += Pattern1 + FuncNameAdd + Pattern2 + FuncArgs + GPPatternEnd;                
            }
            return codeStr;
        }

        public string GetGPTail(bool ComputerContext)
        {
            string codeTail = @"
        <TestDefinition>
            <Tag Name=""Action"" Value=""Update the Policy Config"" />
            <TestDescription>Update the policy on DDC</TestDescription>
            <TimeLimit>00:05:00</TimeLimit>
            <Script Name=""gpupdate.exe"" PreValidate=""false"" Copy=""false""
                    Args=""/force"" />
            <ExecuteOn>DDC</ExecuteOn>
            <UserContext>Administrator</UserContext>
        </TestDefinition>
";
            if (ComputerContext)
            {
                codeTail += @"
        <TestDefinition>
            <TestDescription>Restart all VDA machines</TestDescription>
            <TimeLimit>00:01:00</TimeLimit>
            <Script PreValidate=""false"" Copy=""false"" Name=""shutdown.exe"" Args=""-r -t 10 -f"" />
            <RunParallel>true</RunParallel>
            <ExecuteOn>TSVDA,VDA</ExecuteOn>
        </TestDefinition>

        <TestDefinition>
            <Tag Name=""Action"" Value=""Pause the Test 180 senconds"" />
            <TestDescription>Wait for the recovery of the session</TestDescription>
            <TimeLimit>00:05:00</TimeLimit>
            <Function Name=""Sleep""
                      Args=""180 -Verbose"" />
            <ExecuteOn>COORDINATOR</ExecuteOn>
            <UserContext>Administrator</UserContext>
        </TestDefinition>
";
            }
            else
            { 
                codeTail += @"
        <TestDefinition>
            <Tag Name=""Action"" Value=""Logoff all Sessions"" />
            <TestDescription>Logoff all Sessions</TestDescription>
            <TimeLimit>00:05:00</TimeLimit>
            <Function Name=""Disconnect-BrokerSessions""
                      Args=""-Logoff -verbose""
                      ModuleName=""DDCConfiguration""
                      ModuleBasePath=""TestAPI""/>
            <ExecuteOn>DDC</ExecuteOn>
            <UserContext>Administrator</UserContext>
        </TestDefinition>

        <TestDefinition>
            <Tag Name=""Action"" Value=""Pause the Test 30 senconds"" />
            <TestDescription>Wait for the recovery of the session</TestDescription>
            <TimeLimit>00:05:00</TimeLimit>
            <Function Name=""Sleep""
                      Args=""30 -Verbose"" />
            <ExecuteOn>COORDINATOR</ExecuteOn>
            <UserContext>Administrator</UserContext>
        </TestDefinition>
";
            }
            return codeTail;
        }


    }
}
