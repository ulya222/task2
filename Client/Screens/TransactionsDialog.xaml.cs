using System.Windows;
using DataVault.Core.Entities;

namespace DataVault.Client.Screens;

public partial class TransactionsDialog : Window
{
    public TransactionsDialog(List<ResourceTransaction> transactions, int? resourceId)
    {
        InitializeComponent();
        TransactionsGrid.ItemsSource = transactions;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
