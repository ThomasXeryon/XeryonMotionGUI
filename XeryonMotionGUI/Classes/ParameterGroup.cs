using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Classes;
public class ParameterGroup
{
    public string Category
    {
        get; set;
    }
    public ObservableCollection<Parameter> Parameters
    {
        get; set;
    }
}

