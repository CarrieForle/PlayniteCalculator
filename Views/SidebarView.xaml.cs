using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Calculator
{
    public partial class SidebarView : UserControl
    {
        public IDictionary<Game, HistoricalLowOutput> HistoricalLows { get; }
        //public double TotalSpentIfRegularPrice {
        //    get => HistoricalLows.Sum(pair => pair.Value.price);
        //}

        //public double TotalSpentIfDiscountedPrice
        //{
        //    get => HistoricalLows.Sum(pair => pair.Value.lowPrice);
        //}

        public SidebarView(IDictionary<Game, HistoricalLowOutput> historicalLows)
        {
            HistoricalLows = historicalLows;
			DataContext = this;
            InitializeComponent();
        }
    }
}
