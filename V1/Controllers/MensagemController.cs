using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class MensagemController : ControllerBase
    {
        private readonly IMensagemRepository _mensagemrepository;

        public MensagemController(IMensagemRepository mensagemrepository)
        {
            _mensagemrepository = mensagemrepository;
        }


        [Authorize]
        [HttpGet("{usuarioUmId}/{usuarioDoisId}")]
        public ActionResult ObterMensagem(string usuarioUmId,string usuarioDoisId)
        {
            if (usuarioUmId == usuarioDoisId)
                return UnprocessableEntity();

           
            return Ok(_mensagemrepository.ObterMensagem(usuarioUmId, usuarioDoisId));
        }


        [Authorize]
        [HttpPost("")]
        public ActionResult Cadastrar([FromBody]Mensagem mensagem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _mensagemrepository.Cadastrar(mensagem);
                    return Ok(mensagem);
                }
                catch (Exception ex)
                {

                    return UnprocessableEntity(ex);
                }
            }else
            {
                return UnprocessableEntity(ModelState);
            }
            return Ok();
        }
    }
}