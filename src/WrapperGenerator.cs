using System;
using System.Text;

namespace BlockEngine
{
    public static class WrapperGenerator
    {
        // Shared state separator marker
        private const string StateMarker = "__BLOCK_STATE__:";

        public static string GenerateWrapper(string lang, string code)
        {
            lang = (lang ?? "").ToLower().Trim();
            if (lang == "py") lang = "python";
            else if (lang == "javascript") lang = "js";
            else if (lang == "powershell") lang = "ps";
            else if (lang == "rb") lang = "ruby";
            else if (lang == "pl") lang = "perl";
            else if (lang == "sh") lang = "bash";
            else if (lang == "rs") lang = "rust";
            else if (lang == "golang") lang = "go";
            else if (lang == "typescript") lang = "ts";
            else if (lang == "csharp") lang = "cs";

#if BLOCK_LITE
            if (lang == "python")
            {
                return "import json,sys,os\n" +
                       "_s=os.environ.get('BLOCK_STATE_JSON','')\n" +
                       "if not _s and os.environ.get('BLOCK_STATE_FILE'):\n" +
                       "  try: _s=open(os.environ['BLOCK_STATE_FILE'],'r',encoding='utf-8').read()\n" +
                       "  except: pass\n" +
                       "_st=json.loads(_s) if _s.strip() else {}\n" +
                       "if os.environ.get('BLOCK_NETWORK_BLOCKED')=='1':\n" +
                       "  import socket as _block_socket, builtins as _block_builtins, importlib as _block_importlib\n" +
                       "  def _block_network(*a,**k): raise RuntimeError('Block network access is disabled')\n" +
                       "  _block_socket.socket=_block_network\n" +
                       "  _block_socket.create_connection=_block_network\n" +
                       "  _block_denied={'socket','_socket','ssl','http','urllib','ftplib','smtplib','telnetlib','requests','aiohttp','subprocess','multiprocessing'}\n" +
                       "  _block_import=_block_builtins.__import__\n" +
                       "  def _block_guarded_import(name,*a,**k):\n" +
                       "    if name.split('.')[0] in _block_denied: raise RuntimeError('Block advisory network guard denied module: '+name)\n" +
                       "    return _block_import(name,*a,**k)\n" +
                       "  _block_reload=_block_importlib.reload\n" +
                       "  def _block_guarded_reload(module):\n" +
                       "    if getattr(module,'__name__','').split('.')[0] in _block_denied: raise RuntimeError('Block advisory network guard denied module reload')\n" +
                       "    return _block_reload(module)\n" +
                       "  _block_builtins.__import__=_block_guarded_import\n" +
                       "  _block_importlib.reload=_block_guarded_reload\n" +
                       "_block_reserved={'__builtins__','__name__','__doc__','__package__','__loader__','__spec__','__annotations__','__file__','__cached__'}\n" +
                       "[globals().__setitem__(k,v) for k,v in _st.items() if k.isidentifier() and k not in _block_reserved]\n" +
                       code + "\n" +
                       "_ns={k:v for k,v in globals().items() if not k.startswith('_') and k not in('json','sys','os') and type(v) in(int,float,str,bool,list,dict)}\n" +
                       "_nsj=json.dumps(_ns)\n" +
                       "if os.environ.get('BLOCK_STATE_OUT'):\n" +
                       "  try: open(os.environ['BLOCK_STATE_OUT'],'w',encoding='utf-8').write(_nsj)\n" +
                       "  except: pass\n" +
                       "print('\\n" + StateMarker + "'+_nsj)\n";
            }
            if (lang == "js")
            {
                return "const fs=require('fs');\n" +
                       "let _s=process.env.BLOCK_STATE_JSON||'';\n" +
                       "if(!_s&&process.env.BLOCK_STATE_FILE){try{_s=fs.readFileSync(process.env.BLOCK_STATE_FILE,'utf8');}catch(e){}}\n" +
                       "let _st={};try{_st=_s.trim()?JSON.parse(_s):{};}catch(e){}\n" +
                       "const _base=new Set(Object.getOwnPropertyNames(global));\n" +
                       "if(process.env.BLOCK_NETWORK_BLOCKED==='1'){const _net=require('net');const _tls=require('tls');const _http=require('http');const _https=require('https');const _dgram=require('dgram');const _dns=require('dns');const _Module=require('module');const _blocked=()=>{throw new Error('Block network access is disabled');};_net.connect=_blocked;_net.createConnection=_blocked;_tls.connect=_blocked;_http.request=_blocked;_http.get=_blocked;_https.request=_blocked;_https.get=_blocked;_dgram.createSocket=_blocked;_dns.lookup=_blocked;const _load=_Module._load;const _denied=new Set(['net','tls','http','https','http2','dgram','dns','child_process','cluster','worker_threads']);_Module._load=function(r,p,m){if(_denied.has(String(r).replace(/^node:/,'')))_blocked();return _load.call(this,r,p,m);};if(typeof global.fetch==='function')global.fetch=_blocked;if(typeof global.WebSocket==='function')global.WebSocket=_blocked;}\n" +
                       "for(let k in _st)if(/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(k))global[k]=_st[k];\n" +
                       code + "\n" +
                       "const _clone=(v)=>{let c=new Set();let s=JSON.stringify(v,(_k,x)=>{if(typeof x==='object'&&x!==null){if(c.has(x))return;c.add(x);}return x;});return s===undefined?undefined:JSON.parse(s);};\n" +
                       "let _ns={};let _keys=new Set([...Object.keys(_st),...Object.getOwnPropertyNames(global).filter(k=>!_base.has(k))]);for(let k of _keys){try{if(/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(k)){let v=global[k];if(v===null||typeof v==='number'||typeof v==='string'||typeof v==='boolean'||Array.isArray(v)||(typeof v==='object'&&Object.getPrototypeOf(v)===Object.prototype)){let copy=_clone(v);if(copy!==undefined)_ns[k]=copy;}}}catch(e){}}\n" +
                       "let _nsj=JSON.stringify(_ns);\n" +
                       "if(process.env.BLOCK_STATE_OUT){try{fs.writeFileSync(process.env.BLOCK_STATE_OUT,_nsj,'utf8');}catch(e){}}\n" +
                       "console.log('\\n" + StateMarker + "'+_nsj);\n";
            }
            return code;
#else
            if (lang == "python")
            {
                return 
#if BLOCK_PLUS
                       "import json, sys, os, datetime, math\n" +
                       "from pathlib import Path\n" +
#else
                       "import json, sys, os\n" +
#endif
                       "state_json = os.environ.get('BLOCK_STATE_JSON', '')\n" +
                       "if not state_json and os.environ.get('BLOCK_STATE_FILE'):\n" +
                       "    try:\n" +
                       "        with open(os.environ['BLOCK_STATE_FILE'], 'r', encoding='utf-8') as _f:\n" +
                       "            state_json = _f.read()\n" +
                       "    except Exception:\n" +
                       "        pass\n" +
                       "state = json.loads(state_json) if state_json.strip() else {}\n" +
                       "if os.environ.get('BLOCK_NETWORK_BLOCKED') == '1':\n" +
                       "    import socket as _block_socket, builtins as _block_builtins, importlib as _block_importlib\n" +
                       "    def _block_network(*args, **kwargs):\n" +
                       "        raise RuntimeError('Block network access is disabled')\n" +
                       "    _block_socket.socket = _block_network\n" +
                       "    _block_socket.create_connection = _block_network\n" +
                       "    _block_denied = {'socket', '_socket', 'ssl', 'http', 'urllib', 'ftplib', 'smtplib', 'telnetlib', 'requests', 'aiohttp', 'subprocess', 'multiprocessing'}\n" +
                       "    _block_original_import = _block_builtins.__import__\n" +
                       "    def _block_guarded_import(name, *args, **kwargs):\n" +
                       "        if name.split('.')[0] in _block_denied:\n" +
                       "            raise RuntimeError('Block advisory network guard denied module: ' + name)\n" +
                       "        return _block_original_import(name, *args, **kwargs)\n" +
                       "    _block_original_reload = _block_importlib.reload\n" +
                       "    def _block_guarded_reload(module):\n" +
                       "        if getattr(module, '__name__', '').split('.')[0] in _block_denied:\n" +
                       "            raise RuntimeError('Block advisory network guard denied module reload')\n" +
                       "        return _block_original_reload(module)\n" +
                       "    _block_builtins.__import__ = _block_guarded_import\n" +
                       "    _block_importlib.reload = _block_guarded_reload\n" +
                       "_block_reserved = {'__builtins__', '__name__', '__doc__', '__package__', '__loader__', '__spec__', '__annotations__', '__file__', '__cached__'}\n" +
                       "for k, v in state.items():\n" +
                       "    if k.isidentifier() and k not in _block_reserved:\n" +
                       "        globals()[k] = v\n" +
                       "try:\n" +
                       string.Join("\n", Array.ConvertAll(code.Split('\n'), line => "    " + line)) + "\n" +
                       "except Exception as e:\n" +
                        "    print('Python Error: ' + str(e), file=sys.stderr)\n" +
                        "    sys.exit(1)\n" +
                       "new_state = {}\n" +
                       "for k, v in list(globals().items()):\n" +
                       "    if not k.startswith('__') and k not in ('json', 'sys', 'os', 'datetime', 'math', 'Path', 'state', 'state_json', 'new_state') and type(v) in (int, float, str, bool, list, dict):\n" +
                       "        new_state[k] = v\n" +
                       "new_state_json = json.dumps(new_state)\n" +
                       "if os.environ.get('BLOCK_STATE_OUT'):\n" +
                       "    try:\n" +
                       "        with open(os.environ['BLOCK_STATE_OUT'], 'w', encoding='utf-8') as _f:\n" +
                       "            _f.write(new_state_json)\n" +
                       "    except Exception:\n" +
                       "        pass\n" +
                       "print('\\n" + StateMarker + "' + new_state_json)\n";
            }
            if (lang == "php")
            {
                return "<?php\n" +
                       "$state_json = getenv('BLOCK_STATE_JSON') ?: '';\n" +
                       "if (!$state_json && getenv('BLOCK_STATE_FILE') && file_exists(getenv('BLOCK_STATE_FILE'))) {\n" +
                       "    $state_json = file_get_contents(getenv('BLOCK_STATE_FILE'));\n" +
                       "}\n" +
                       "$BLOCK_STATE = json_decode($state_json, true) ?: [];\n" +
                       "extract(array_filter($BLOCK_STATE, function($k){ return preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $k); }, ARRAY_FILTER_USE_KEY));\n" +
                       "try {\n" +
                       code + "\n" +
                        "} catch (Throwable $e) { fwrite(STDERR, 'PHP Error: ' . $e->getMessage() . \"\\n\"); exit(1); }\n" +
                       "$BLOCK_NEW_STATE = [];\n" +
                       "$vars = get_defined_vars();\n" +
                       "foreach ($vars as $k => $v) {\n" +
                       "    if (!in_array($k, ['_GET', '_POST', '_COOKIE', '_FILES', '_SERVER', '_ENV', '_REQUEST', 'GLOBALS', 'argv', 'argc', 'BLOCK_STATE', 'BLOCK_NEW_STATE', 'state_json', 'vars'])) {\n" +
                       "        if (is_int($v) || is_float($v) || is_string($v) || is_bool($v) || is_array($v)) $BLOCK_NEW_STATE[$k] = $v;\n" +
                       "    }\n" +
                       "}\n" +
                       "$new_state_json = json_encode($BLOCK_NEW_STATE);\n" +
                       "if (getenv('BLOCK_STATE_OUT')) { @file_put_contents(getenv('BLOCK_STATE_OUT'), $new_state_json); }\n" +
                       "echo \"\\n" + StateMarker + "\" . $new_state_json . \"\\n\";\n";
            }
            if (lang == "js")
            {
                return "const fs = require('fs');\n" +
                       "let state_json = process.env.BLOCK_STATE_JSON || '';\n" +
                       "if (!state_json && process.env.BLOCK_STATE_FILE) {\n" +
                       "    try { state_json = fs.readFileSync(process.env.BLOCK_STATE_FILE, 'utf8'); } catch(e) {}\n" +
                       "}\n" +
                       "let state = {};\n" +
                       "try { state = state_json.trim() ? JSON.parse(state_json) : {}; } catch(e) {}\n" +
                       "const block_global_baseline = new Set(Object.getOwnPropertyNames(global));\n" +
                       "if (process.env.BLOCK_NETWORK_BLOCKED === '1') { const _net = require('net'); const _tls = require('tls'); const _http = require('http'); const _https = require('https'); const _dgram = require('dgram'); const _dns = require('dns'); const _Module = require('module'); const _blocked = () => { throw new Error('Block network access is disabled'); }; _net.connect = _blocked; _net.createConnection = _blocked; _tls.connect = _blocked; _http.request = _blocked; _http.get = _blocked; _https.request = _blocked; _https.get = _blocked; _dgram.createSocket = _blocked; _dns.lookup = _blocked; const _originalLoad = _Module._load; const _deniedModules = new Set(['net', 'tls', 'http', 'https', 'http2', 'dgram', 'dns', 'child_process', 'cluster', 'worker_threads']); _Module._load = function(request, parent, isMain) { if (_deniedModules.has(String(request).replace(/^node:/, ''))) _blocked(); return _originalLoad.call(this, request, parent, isMain); }; if (typeof global.fetch === 'function') global.fetch = _blocked; if (typeof global.WebSocket === 'function') global.WebSocket = _blocked; }\n" +
                       "for (let k in state) if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(k)) global[k] = state[k];\n" +
                       "try {\n" +
                       code + "\n" +
                        "} catch(e) { console.error('JS Error:', e.message); process.exitCode = 1; }\n" +
                       "const block_clone_json = (value) => {\n" +
                       "    let seen = new Set();\n" +
                       "    let json = JSON.stringify(value, (key, item) => {\n" +
                       "        if (typeof item === 'object' && item !== null) { if (seen.has(item)) return; seen.add(item); }\n" +
                       "        return item;\n" +
                       "    });\n" +
                       "    return json === undefined ? undefined : JSON.parse(json);\n" +
                       "};\n" +
                       "let new_state = {};\n" +
                       "let block_candidate_keys = new Set([...Object.keys(state), ...Object.getOwnPropertyNames(global).filter(k => !block_global_baseline.has(k))]);\n" +
                       "for (let k of block_candidate_keys) {\n" +
                       "    try {\n" +
                       "        if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(k)) continue;\n" +
                       "        let v = global[k];\n" +
                       "        let isPlainObject = v !== null && typeof v === 'object' && Object.getPrototypeOf(v) === Object.prototype;\n" +
                       "        if (v === null || typeof v === 'number' || typeof v === 'string' || typeof v === 'boolean' || Array.isArray(v) || isPlainObject) {\n" +
                       "            let copy = block_clone_json(v);\n" +
                       "            if (copy !== undefined) new_state[k] = copy;\n" +
                       "        }\n" +
                       "    } catch(e) {}\n" +
                       "}\n" +
                       "let new_state_json = JSON.stringify(new_state);\n" +
                       "if (process.env.BLOCK_STATE_OUT) { try { fs.writeFileSync(process.env.BLOCK_STATE_OUT, new_state_json, 'utf8'); } catch(e) {} }\n" +
                       "console.log('\\n" + StateMarker + "' + new_state_json);\n";
            }
            if (lang == "lua")
            {
                return "-- Pure Lua Lightweight JSON Fallback Parser & Serializer\n" +
                       "local function parse_simple_json(str)\n" +
                       "    if not str or str == '' then return {} end\n" +
                       "    local ok, json = pcall(require, 'json')\n" +
                       "    if ok and json then return json.decode(str) end\n" +
                       "    local res = {}\n" +
                       "    for k, v in string.gmatch(str, '\"([%w_]+)\"%s*:%s*([%d%.]+)') do res[k] = tonumber(v) end\n" +
                       "    for k, v in string.gmatch(str, '\"([%w_]+)\"%s*:%s*\"([^\"]*)\"') do res[k] = v end\n" +
                       "    for k, v in string.gmatch(str, '\"([%w_]+)\"%s*:%s*(true)') do res[k] = true end\n" +
                       "    for k, v in string.gmatch(str, '\"([%w_]+)\"%s*:%s*(false)') do res[k] = false end\n" +
                       "    return res\n" +
                       "end\n" +
                       "local function encode_simple_json(t)\n" +
                       "    local ok, json = pcall(require, 'json')\n" +
                       "    if ok and json then return json.encode(t) end\n" +
                       "    local parts = {}\n" +
                       "    for k, v in pairs(t) do\n" +
                       "        if type(v) == 'number' then table.insert(parts, string.format('\"%s\":%s', k, tostring(v)))\n" +
                       "        elseif type(v) == 'string' then table.insert(parts, string.format('\"%s\":\"%s\"', k, v:gsub('\"', '\\\"')))\n" +
                       "        elseif type(v) == 'boolean' then table.insert(parts, string.format('\"%s\":%s', k, tostring(v)))\n" +
                       "        end\n" +
                       "    end\n" +
                       "    return '{' .. table.concat(parts, ',') .. '}'\n" +
                       "end\n" +
                       "local state_json = os.getenv('BLOCK_STATE_JSON') or ''\n" +
                       "if state_json == '' and os.getenv('BLOCK_STATE_FILE') then\n" +
                       "    local f = io.open(os.getenv('BLOCK_STATE_FILE'), 'r')\n" +
                       "    if f then state_json = f:read('*a'); f:close() end\n" +
                       "end\n" +
                       "local state = parse_simple_json(state_json)\n" +
                       "if type(state) == 'table' then\n" +
                       "    for k, v in pairs(state) do _G[k] = v end\n" +
                       "end\n" +
                       "local err_status, err_msg = pcall(function()\n" +
                       code + "\n" +
                       "end)\n" +
                       "if not err_status then\n" +
                       "    io.stderr:write('Lua Error: ' .. tostring(err_msg) .. '\\n')\n" +
                       "    os.exit(1)\n" +
                       "end\n" +
                       "local new_state = {}\n" +
                       "for k, v in pairs(_G) do\n" +
                       "    if type(k) == 'string' and not k:find('^_') and k ~= 'state_json' and k ~= 'state' and k ~= 'new_state' and k ~= 'parse_simple_json' and k ~= 'encode_simple_json' then\n" +
                       "        if type(v) == 'number' or type(v) == 'string' or type(v) == 'boolean' then\n" +
                       "            new_state[k] = v\n" +
                       "        end\n" +
                       "    end\n" +
                       "end\n" +
                       "local out_json = encode_simple_json(new_state)\n" +
                       "if os.getenv('BLOCK_STATE_OUT') then local sf = io.open(os.getenv('BLOCK_STATE_OUT'), 'w'); if sf then sf:write(out_json); sf:close() end end\n" +
                       "print('\\n" + StateMarker + "' .. out_json)\n";
            }
            if (lang == "ruby")
            {
                return "require 'json'\n" +
                       "state_json = ENV['BLOCK_STATE_JSON'] || ''\n" +
                       "if state_json.empty? && ENV['BLOCK_STATE_FILE'] && File.exist?(ENV['BLOCK_STATE_FILE'])\n" +
                       "  state_json = File.read(ENV['BLOCK_STATE_FILE'])\n" +
                       "end\n" +
                       "begin\n" +
                       "  state = JSON.parse(state_json)\n" +
                       "  state.each { |k, v| TOPLEVEL_BINDING.local_variable_set(k.to_sym, v) if k =~ /^[a-zA-Z_][a-zA-Z0-9_]*$/ }\n" +
                       "rescue Exception => e\n" +
                       "end\n" +
                       "begin\n" +
                       code + "\n" +
                       "rescue Exception => e\n" +
                        "  $stderr.puts 'Ruby Error: ' + e.message\n" +
                        "  exit(1)\n" +
                       "end\n" +
                       "new_state = {}\n" +
                       "TOPLEVEL_BINDING.local_variables.each do |v|\n" +
                       "  val = TOPLEVEL_BINDING.local_variable_get(v)\n" +
                       "  new_state[v.to_s] = val if [Integer, Float, String, TrueClass, FalseClass, Array, Hash].include?(val.class)\n" +
                       "end\n" +
                       "out_json = JSON.generate(new_state)\n" +
                       "if ENV['BLOCK_STATE_OUT']; File.write(ENV['BLOCK_STATE_OUT'], out_json) rescue nil; end\n" +
                       "puts '\\n" + StateMarker + "' + out_json\n";
            }
            if (lang == "ps")
            {
                return "$state_json = $env:BLOCK_STATE_JSON\n" +
                       "if (-not $state_json -and $env:BLOCK_STATE_FILE -and (Test-Path $env:BLOCK_STATE_FILE)) {\n" +
                       "    $state_json = Get-Content -Path $env:BLOCK_STATE_FILE -Raw\n" +
                       "}\n" +
                       "if ($state_json) {\n" +
                       "    try {\n" +
                       "        $state = ConvertFrom-Json $state_json\n" +
                       "        foreach ($prop in $state.psobject.Properties) {\n" +
                       "            Set-Variable -Name $prop.Name -Value $prop.Value -Scope Global\n" +
                       "        }\n" +
                       "    } catch {}\n" +
                       "}\n" +
                       "try {\n" +
                       code + "\n" +
                        "} catch { [Console]::Error.WriteLine(\"PowerShell Error: \" + $_.Exception.Message); exit 1 }\n" +
                       "$new_state = @{}\n" +
                       "Get-Variable -Scope Global | ForEach-Object {\n" +
                       "    if ($_.Name -notmatch '^_|^BLOCK_|^state_|^PID$|^HOME$|^PATH$|^NULL$|^true$|^false$') {\n" +
                       "        if ($_.Value -is [int] -or $_.Value -is [string] -or $_.Value -is [bool] -or $_.Value -is [double] -or $_.Value -is [array] -or $_.Value -is [hashtable]) {\n" +
                       "            $new_state[$_.Name] = $_.Value\n" +
                       "        }\n" +
                       "    }\n" +
                       "}\n" +
                       "$out_json = $($new_state | ConvertTo-Json -Compress)\n" +
                       "if ($env:BLOCK_STATE_OUT) { Set-Content -Path $env:BLOCK_STATE_OUT -Value $out_json -ErrorAction SilentlyContinue }\n" +
                       "Write-Output \"`n" + StateMarker + "$out_json\"\n";
            }
            return code;
#endif
        }
    }
}
