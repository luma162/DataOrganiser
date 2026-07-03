using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganiser.Services;

public class DialogueService()
{
    public string? PickFolder()
    {
        using var dialog = new FolderBrowserDialog();
        return dialog.ShowDialog() == DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}
