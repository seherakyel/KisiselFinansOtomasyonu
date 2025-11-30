using DevExpress.XtraEditors;
using DevExpress.XtraScheduler;
using KisiselFinans.Business.Services;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Controls;

public class CalendarControl : XtraUserControl
{
    private readonly int _userId;
    private SchedulerControl _scheduler = null!;
    private SchedulerDataStorage _storage = null!;

    public CalendarControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        _storage = new SchedulerDataStorage();
        _scheduler = new SchedulerControl
        {
            Dock = DockStyle.Fill,
            DataStorage = _storage,
            Start = DateTime.Now
        };

        _scheduler.ActiveViewType = SchedulerViewType.Month;
        _scheduler.MonthView.ShowWeekend = true;

        Controls.Add(_scheduler);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new ScheduledTransactionService(unitOfWork);

            var scheduled = await service.GetUserScheduledTransactionsAsync(_userId);
            var appointments = new List<Appointment>();

            foreach (var item in scheduled)
            {
                var apt = _storage.CreateAppointment(AppointmentType.Normal);
                apt.Subject = $"{item.Category.CategoryName}: {item.Amount:N2} TRY";
                apt.Description = item.Description;
                apt.Start = item.NextExecutionDate;
                apt.End = item.NextExecutionDate.AddHours(1);
                apt.LabelKey = item.Category.Type == 1 ? 3 : 2; // Yeşil: Gelir, Kırmızı: Gider

                appointments.Add(apt);
            }

            _storage.Appointments.Items.Clear();
            foreach (var apt in appointments)
            {
                _storage.Appointments.Items.Add(apt);
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}

