using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.Models
{
    public class CollectionModel: ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
