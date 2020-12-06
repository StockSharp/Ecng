namespace Ecng.Xaml
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Ecng.ComponentModel;

    public abstract class NotifiableObjectGuiWrapper<T> : CustomObjectWrapper<T> where T : class, INotifyPropertyChanged
    {
        protected NotifiableObjectGuiWrapper(T obj) : base(obj)
            => Obj.PropertyChanged += (_, args) =>
            {
                if(NeedToNotify(args.PropertyName))
                    GuiDispatcher.GlobalDispatcher.Dispatcher.GuiAsync(() => OnPropertyChanged(args.PropertyName));
            };

        protected virtual bool NeedToNotify(string propName) => true;

        protected override IEnumerable<EventDescriptor> OnGetEvents()
        {
            var myEventDescriptor = TypeDescriptor.GetEvents(this, true).OfType<EventDescriptor>().First(ed => ed.Name == nameof(PropertyChanged));

            return
                base.OnGetEvents()
                    .Where(ed => ed.Name != nameof(PropertyChanged))
                    .Concat(new[] { myEventDescriptor });
        }
    }
}