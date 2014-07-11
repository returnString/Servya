using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace Servya
{
	public struct WebInterfaceParam
	{
		public Type Type { get; private set; }
		public string Name { get; private set; }

		public WebInterfaceParam(Type type, string name)
			: this()
		{
			Type = type;
			Name = name;
		}
	}

	public class WebInterfaceConfig
	{
		public Func<string, string> PathModifier { get; set; }
		public Dictionary<string, WebInterfaceParam> Swap { get; set; }
		public Dictionary<string, string> Defaults { get; set; }
		public Func<MethodInfo, WebInterfaceParam[]> ExtraParamCreator { get; set; }
		public Func<Type, Type> ReturnTypeModifier { get; set; }

		public static readonly WebInterfaceConfig Default = new WebInterfaceConfig();
	}

	// TODO: Shouldn't re-discover services, stream through ReflectionRouter
	internal static class WebInterface
	{
		private static readonly HashSet<Type> IgnoredTypes = new HashSet<Type> { typeof(IHttpContext) };

		public static string Create(WebInterfaceConfig config)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<html>{0}<body>", JS);

			sb.Append("<div style='width:100%;'><div class='left' style='float:left; width:20%;'>");

			foreach (var type in Reflection.GetAllTypes().OrderBy(t => t.Name))
			{
				ServiceAttribute serviceAttr;
				if (!type.TryGetAttribute(out serviceAttr))
					continue;

				var serviceName = ServiceAttribute.GetName(type, serviceAttr).ToLower();

				foreach (var method in type.GetMethods())
				{
					RouteAttribute methodAttr;
					if (!method.TryGetAttribute(out methodAttr))
						continue;

					var anchor = serviceName + method.Name;
					var url = string.Format("/{0}/{1}", serviceName, (methodAttr.Path ?? method.Name).ToLower());

					var returnType = method.ReturnType;
					if (returnType.IsSubclassOf(typeof(Task)) && returnType != typeof(Task))
						returnType = returnType.GetGenericArguments()[0];

					if (config.ReturnTypeModifier != null)
						returnType = config.ReturnTypeModifier(returnType);

					sb.AppendFormat("<h3>{1} <a href='#{0}' id='{0}'>{2}</a> => {3}</h3>", anchor, methodAttr.Verb.ToString().ToUpper(), url, WebUtility.HtmlEncode(returnType.GetFriendlyName()));

					if (config.PathModifier != null)
						url = config.PathModifier(url);

					sb.AppendFormat("<form method='{0}' action='{1}' onsubmit='return submitForm(this)'/><table>", methodAttr.Verb, url);

					var methodParams = method.GetParameters();
					var paramNames = new HashSet<string>();

					Action<string, string, Type> addParam = (name, value, paramType) =>
					{
						if (!paramNames.Add(name))
							return;

						sb.AppendFormat("<tr><td>{0} ({1})</td> <td><input type='text' name='{0}' {2}/></td></tr>", name, paramType.GetFriendlyName(), value);
					};

					foreach (var param in methodParams)
					{
						if (IgnoredTypes.Contains(param.ParameterType))
							continue;

						WebInterfaceParam webParam;
						if (config.Swap == null || !config.Swap.TryGetValue(param.Name, out webParam))
							webParam = new WebInterfaceParam(param.ParameterType, param.Name);

						var value = string.Empty;

						object defaultValue;
						if (param.TryGetDefaultValue(out defaultValue))
						{
							value = defaultValue.ToString();
						}
						else if (config.Defaults != null)
						{
							config.Defaults.TryGetValue(webParam.Name, out value);
						}

						value = string.Format("value='{0}'", value);

						addParam(webParam.Name, value, webParam.Type);
					}

					if (config.ExtraParamCreator != null)
					{
						var extraParams = config.ExtraParamCreator(method);

						if (extraParams != null)
						{
							foreach (var extraParam in extraParams)
								addParam(extraParam.Name, "", extraParam.Type);
						}
					}

					sb.Append("</table><input type='submit' value='Submit'/></form>");
				}

				sb.Append("<hr/>");
			}

			sb.Append("</div>");
			sb.Append("<div class='right' style='float:right; width:80%;'><br/><button type='button' onclick='clearResult()'>Clear</button>");
			sb.Append("<hr/><p id='result' style='white-space: pre;'/></div></div>");
			sb.Append("</body></html>");
			return sb.ToString();
		}

		private const string JS =
		@"<head>
			<style>
				.right, .left {
					overflow:auto; /* This will add a scroll when required */
					width:50%;
					float:left;
					height: 100%
				}
				body {
					overflow:hidden;
				}
				a {
					color: inherit;
					outline: 0;
				}
				pre {
					outline: 1px solid #ccc;
					padding: 5px;
					margin: 5px;
				}
				.string { color: green; }
				.number { color: darkorange; }
				.boolean { color: blue; }
				.null { color: magenta; }
				.key { color: red; }
			</style>
			<script type='text/javascript'>
				if (!String.prototype.encodeHTML) {
					String.prototype.encodeHTML = function () {
					return this.replace(/&/g, '&amp;')
								.replace(/</g, '&lt;')
								.replace(/>/g, '&gt;')
								.replace(/""/g, '&quot;')
								.replace(/'/g, '&apos;');
					};
				}

				function syntaxHighlight(json) {
					json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
					return json.replace(/(""(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\""])*""(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
						var cls = 'number';
						if (/^""/.test(match)) {
							if (/:$/.test(match)) {
								cls = 'key';
							} else {
								cls = 'string';
							}
						} else if (/true|false/.test(match)) {
							cls = 'boolean';
						} else if (/null/.test(match)) {
							cls = 'null';
						}
						return '<span class=""' + cls + '"">' + match + '</span>';
					});
				}

				function serialize(form) {
				    'use strict';
				    var i, j, len, jLen, formElement, q = [];
				    function addNameValue(name, value) {
						if (value)
					        q.push(encodeURIComponent(name) + '=' + encodeURIComponent(value));
				    }
				    if (!form || !form.nodeName || form.nodeName.toLowerCase() !== 'form') {
				        throw 'You must supply a form element';
				    }
				    for (i = 0, len = form.elements.length; i < len; i++) {
				        formElement = form.elements[i];
				        if (formElement.name === '' || formElement.disabled) {
				            continue;
				        }
				        switch (formElement.nodeName.toLowerCase()) {
				        case 'input':
				            switch (formElement.type) {
				            case 'text':
				            case 'hidden':
				            case 'password':
				            case 'button': // Not submitted when submitting form manually, though jQuery does serialize this and it can be an HTML4 successful control
				            case 'submit':
				                addNameValue(formElement.name, formElement.value);
				                break;
				            case 'checkbox':
				            case 'radio':
				                if (formElement.checked) {
				                    addNameValue(formElement.name, formElement.value);
				                }
				                break;
				            case 'file':
				                // addNameValue(formElement.name, formElement.value); // Will work and part of HTML4 ""successful controls"", but not used in jQuery
				                break;
				            case 'reset':
				                break;
				            }
				            break;
				        case 'textarea':
				            addNameValue(formElement.name, formElement.value);
				            break;
				        case 'select':
				            switch (formElement.type) {
				            case 'select-one':
				                addNameValue(formElement.name, formElement.value);
				                break;
				            case 'select-multiple':
				                for (j = 0, jLen = formElement.options.length; j < jLen; j++) {
				                    if (formElement.options[j].selected) {
				                        addNameValue(formElement.name, formElement.options[j].value);
				                    }
				                }
				                break;
				            }
				            break;
				        case 'button': // jQuery does not submit these, though it is an HTML4 successful control
				            switch (formElement.type) {
				            case 'reset':
				            case 'submit':
				            case 'button':
				                addNameValue(formElement.name, formElement.value);
				                break;
				            }
				            break;
				        }
				    }
				    return q.join('&');
				}

				function getResultElem()
				{
					return document.getElementById('result');
				}
				
				function submitForm(formElem)
				{
					var url = formElem.action;
					var query = serialize(formElem);

					if (formElem.method == 'get') {
						url = url + '?' + query;
					}

					var startTime = new Date().getTime();

					var xhr = new XMLHttpRequest();
					xhr.onload = function() {
						var overallTime = new Date().getTime() - startTime;
						var result = getResultElem();

						var finalText = xhr.responseText;

						try
						{
							var json = JSON.parse(finalText);
							finalText = syntaxHighlight(finalText);
						}
						catch (e)
						{
							finalText = finalText.encodeHTML();
						}

						result.innerHTML = '<a href=' + url + '>' + url +'</a> (' + overallTime + 'ms)<br/>' + finalText + '<br/><hr/>' + result.innerHTML;
					}

					xhr.open(formElem.method, url, true);

					if (formElem.method == 'get') {
						xhr.send();
					} else {
						xhr.send(query);
					}

					return false;
				}

				function clearResult()
				{
					getResultElem().innerHTML = '';
				}
			</script>
		</head>";
	}
}
