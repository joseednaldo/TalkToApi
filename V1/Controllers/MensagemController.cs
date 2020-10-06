using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TalkToApi.Helpers.Constants;
using TalkToApi.V1.Models;
using TalkToApi.V1.Models.DTO;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [EnableCors]  // VAI HABOLITAT A POLITICA PADRÃO
    public class MensagemController : ControllerBase
    {
        private readonly IMensagemRepository _mensagemrepository;
        private readonly IMapper _mapper;

        public MensagemController(IMensagemRepository mensagemrepository, IMapper mapper)
        {
            _mensagemrepository = mensagemrepository;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("{usuarioUmId}/{usuarioDoisId}", Name = "MensagemObter")]
        public ActionResult ObterMensagem(string usuarioUmId, string usuarioDoisId, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (usuarioUmId == usuarioDoisId)
                return UnprocessableEntity();


            var mensagens = _mensagemrepository.ObterMensagem(usuarioUmId, usuarioDoisId);

            if (mediaType == CustomMediaType.Hateoas)
            {

                var listaMsg = _mapper.Map<List<Mensagem>, List<MensagemDTO>>(mensagens);
                var lista = new ListaDTO<MensagemDTO>() { Lista = listaMsg };

                lista.Links.Add(new LinkDTO("_self", Url.Link("MensagemObter", new { usuarioUmId = usuarioUmId, usuarioDoisId = usuarioDoisId }), "GET"));

                return Ok(lista);
                //return Ok(_mensagemrepository.ObterMensagem(usuarioUmId, usuarioDoisId));
            }
            else
            {
                return Ok(mensagens);
            }

        }

        [Authorize]
        [HttpPost("", Name = "MensagemCadastrar")]
        public ActionResult Cadastrar([FromBody] Mensagem mensagem, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _mensagemrepository.Cadastrar(mensagem);

                    if (mediaType == CustomMediaType.Hateoas)
                    {

                        var mensagemDTO = _mapper.Map<Mensagem, MensagemDTO>(mensagem);
                        mensagemDTO.Links.Add(new LinkDTO("_self", Url.Link("MensagemCadastrar", null), "POST"));
                        mensagemDTO.Links.Add(new LinkDTO("_atualizacaoParcial", Url.Link("MensagemAtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

                        return Ok(mensagemDTO);
                    }
                    else
                    {
                        return Ok(mensagem);
                    }

                }
                catch (Exception ex)
                {

                    return UnprocessableEntity(ex);
                }
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }

            return Ok();
        }

        [Authorize]
        [HttpPatch("{id}", Name = "MensagemAtualizacaoParcial")]
        public ActionResult AtualizacaoParcial(int id, [FromBody] JsonPatchDocument<Mensagem> JSONPath, [FromHeader(Name = "Accept")] string mediaType)
        {

            if (JSONPath == null)
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

            if (mediaType == CustomMediaType.Hateoas)
            {
                var mensagemDTO = _mapper.Map<Mensagem, MensagemDTO>(mensagem);
                mensagemDTO.Links.Add(new LinkDTO("_self", Url.Link("MensagemAtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

                return Ok(mensagemDTO);
            }
            else
            {
                return Ok(mensagem);
            }
        }




    }
}
