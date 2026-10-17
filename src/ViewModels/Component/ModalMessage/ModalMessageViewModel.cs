using ChamadosTi.ViewModels.Component.Enums;

namespace ChamadosTi.ViewModels.Component.ModalMessage
{
    public class ModalMessageViewModel
    {
        public string ModalId { get; set; } = "ModalPadraoId";
        public string Titulo { get; set; } = "";
        public string Mensagem { get; set; } = "";
        public string? Action { get; set; }
        public string? RouteId { get; set; }
        public string? Method { get; set; }
        public string? BotaoConfirmarType { get; set; }
        public bool BotaoAbrirHabilitado {get; set; } = false;
        public string? BotaoAbrirTexto {get; set; }
        public TipoModal Tipo { get; set; } = TipoModal.Information;
        public TipoBotaoModal Botao { get; set; } = TipoBotaoModal.Ok;
        public string Icone => Tipo switch
        {
            TipoModal.Alert       => "bi-exclamation-circle-fill",
            TipoModal.Information => "bi-info-circle-fill",
            TipoModal.Success     => "bi-check-circle-fill",
            TipoModal.Error       => "bi-x-circle-fill",
            _                           => ""
        }; 

        public string CorFundo => Tipo switch
        {
            TipoModal.Alert       => "bg-warning",
            TipoModal.Information => "bg-info",
            TipoModal.Success     => "bg-success",
            TipoModal.Error       => "bg-danger",
            _                           => ""
        };

        public string BotaoConfirmarTexto => Botao switch
        {
            TipoBotaoModal.Ok or TipoBotaoModal.OkCancelar   => "Botao_Ok",
            TipoBotaoModal.SimNao                              => "Botao_Sim",
            TipoBotaoModal.ConfirmarCancelar                   => "BotaoConfirmar",
            _                                                    => ""
        };

        public string BotaoCancelarTexto => Botao switch
        {
            TipoBotaoModal.OkCancelar or TipoBotaoModal.ConfirmarCancelar => "Botao_Cancelar",
            TipoBotaoModal.SimNao                                           => "Botao_Nao",
            _                                                                 => ""
        };

        public string BotaoAbrirClass => Tipo switch
        {
            TipoModal.Alert       => "btn btn-sm btn-outline-warning w-100 shadow-sm fw-semibold py-2",
            TipoModal.Information => "btn btn-sm btn-outline-info w-100 shadow-sm fw-semibold py-2",
            TipoModal.Success     => "btn btn-sm btn-outline-success w-100 shadow-sm fw-semibold py-2",
            TipoModal.Error       => "btn btn-sm btn-outline-danger w-100 shadow-sm fw-semibold py-2",
            _                           => ""
        };

        public string BotaoAbrirIcone => Tipo switch
        {
            TipoModal.Alert       => "bi bi-exclamation-circle-fill", 
            TipoModal.Information => "bi bi-info-circle-fill",          
            TipoModal.Success     => "bi bi-check-circle-fill",         
            TipoModal.Error       => "bi bi-x-circle-fill",            
            _                           => ""
        };

        public string BotaoConfirmarClass => Tipo switch
        {
            TipoModal.Alert       => "btn btn-sm btn-warning px-3 me-2",
            TipoModal.Information => "btn btn-sm btn-info px-3 me-2",
            TipoModal.Success     => "btn btn-sm btn-success px-3 me-2",
            TipoModal.Error       => "btn btn-sm btn-danger px-3 me-2",
            _                           => ""
        };

        public string BotaoCancelarClass => Botao switch
        {
            TipoBotaoModal.OkCancelar or 
            TipoBotaoModal.SimNao or 
            TipoBotaoModal.ConfirmarCancelar => "btn btn-sm btn-outline-secondary px-3",
            _ => ""
        };

        public string BotaoConfirmarIcone => Botao switch
        {
            TipoBotaoModal.OkCancelar or 
            TipoBotaoModal.ConfirmarCancelar or
            TipoBotaoModal.SimNao => "bi bi-check-circle-fill me-1",
            _                       => ""
        };

        public string BotaoCancelarIcone => Botao switch
        {
            TipoBotaoModal.OkCancelar or 
            TipoBotaoModal.SimNao or 
            TipoBotaoModal.ConfirmarCancelar => "bi bi-x-circle-fill me-1",
            _                                  => ""
        };


    }
}