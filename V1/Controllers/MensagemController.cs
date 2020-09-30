using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
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

        [Authorize]
        [HttpPatch("{id}")]
        public ActionResult AtualizacaoParcial(int id,[FromBody]JsonPatchDocument<Mensagem> JSONPath)
        {

            if(JSONPath == null)
            {
                return BadRequest(); //significa que tem algum dado errado no conteudo que veio do front ex: camo inteiro como valor de string etc..
            }
            /*
             JSONPath -   tem 3 operações
             Add,Remover e replace e path:""

               ex:
               JSONPath - {"op": "add|remove|replace", "path":"texto", "value":"Mensagem substituida"}

            ex:2
             JSONPath - {"op": "add|remove|replace", "path":"excluido", "value":true}
             */

            var mensagem = _mensagemrepository.Obter(id);
                JSONPath.ApplyTo(mensagem);
            mensagem.Atualizado = DateTime.UtcNow;

            _mensagemrepository.Atualizar(mensagem);

            return Ok(mensagem);



        }
    }
}