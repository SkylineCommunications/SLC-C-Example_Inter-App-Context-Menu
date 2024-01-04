using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Skyline.DataMiner.Scripting;
using Skyline.Protocol;

/// <summary>
/// DataMiner QAction Class: ContextMenu.
/// </summary>
public static class QAction
{
    /// <summary>
    /// The QAction entry point.
    /// </summary>
    /// <param name="protocol">Link with SLProtocol process.</param>
    /// /// <param name="contextMenuData">ContextMenu data.</param>
    public static void Run(SLProtocolExt protocol, object contextMenuData)
	{
		try
		{
			ContextMenuHelper.Execute(protocol, contextMenuData as string[]);
		}
		catch (Exception ex)
		{
			protocol.Log($"QA{protocol.QActionID}|{protocol.GetTriggerParameter()}|Run|Exception thrown:{Environment.NewLine}{ex}", LogType.Error, LogLevel.NoLogging);
		}
	}
}