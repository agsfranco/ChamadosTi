using ChamadosTi.ViewModels.Components.ModalMessage;
using Microsoft.AspNetCore.Mvc;

namespace ChamadosTi.ViewComponents
{
    public class ModalMessageViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(ModalMessageViewModel model)
        {
            return View(model);            
        }   
    }
}