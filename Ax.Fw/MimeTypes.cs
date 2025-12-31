using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Ax.Fw;

public static class MimeTypes
{
  private static readonly ConcurrentDictionary<string, string> p_mimeByExtLut = new();

  public static string GetMimeByExtension(string _filename)
  {
    if (_filename.IsNullOrWhiteSpace())
      return Bin.Mime;

    var ext = Path.GetExtension(_filename)?.TrimStart('.').ToLowerInvariant();
    if (ext.IsNullOrEmpty())
      return Bin.Mime;

    return p_mimeByExtLut.GetOrAdd(ext, _ext =>
    {
      var mime = MimeEntry.AllEntries.FirstOrDefault(_entry => _entry.Extensions.Any(_ => _ == _ext));
      return mime?.Mime ?? Bin.Mime;
    });
  }

  ///<summary>ez</summary>
  public static MimeEntry Ez { get; } = new(["ez"], "application/andrew-inset");
  ///<summary>aw</summary>
  public static MimeEntry Aw { get; } = new(["aw"], "application/applixware");
  ///<summary>atom</summary>
  public static MimeEntry Atom { get; } = new(["atom"], "application/atom+xml");
  ///<summary>atomcat</summary>
  public static MimeEntry Atomcat { get; } = new(["atomcat"], "application/atomcat+xml");
  ///<summary>atomsvc</summary>
  public static MimeEntry Atomsvc { get; } = new(["atomsvc"], "application/atomsvc+xml");
  ///<summary>ccxml</summary>
  public static MimeEntry Ccxml { get; } = new(["ccxml"], "application/ccxml+xml");
  ///<summary>cdmia</summary>
  public static MimeEntry Cdmia { get; } = new(["cdmia"], "application/cdmi-capability");
  ///<summary>cdmic</summary>
  public static MimeEntry Cdmic { get; } = new(["cdmic"], "application/cdmi-container");
  ///<summary>cdmid</summary>
  public static MimeEntry Cdmid { get; } = new(["cdmid"], "application/cdmi-domain");
  ///<summary>cdmio</summary>
  public static MimeEntry Cdmio { get; } = new(["cdmio"], "application/cdmi-object");
  ///<summary>cdmiq</summary>
  public static MimeEntry Cdmiq { get; } = new(["cdmiq"], "application/cdmi-queue");
  ///<summary>cu</summary>
  public static MimeEntry Cu { get; } = new(["cu"], "application/cu-seeme");
  ///<summary>davmount</summary>
  public static MimeEntry Davmount { get; } = new(["davmount"], "application/davmount+xml");
  ///<summary>dbk</summary>
  public static MimeEntry Dbk { get; } = new(["dbk"], "application/docbook+xml");
  ///<summary>dssc</summary>
  public static MimeEntry Dssc { get; } = new(["dssc"], "application/dssc+der");
  ///<summary>xdssc</summary>
  public static MimeEntry Xdssc { get; } = new(["xdssc"], "application/dssc+xml");
  ///<summary>ecma</summary>
  public static MimeEntry Ecma { get; } = new(["ecma"], "application/ecmascript");
  ///<summary>emma</summary>
  public static MimeEntry Emma { get; } = new(["emma"], "application/emma+xml");
  ///<summary>epub</summary>
  public static MimeEntry Epub { get; } = new(["epub"], "application/epub+zip");
  ///<summary>exi</summary>
  public static MimeEntry Exi { get; } = new(["exi"], "application/exi");
  ///<summary>pfr</summary>
  public static MimeEntry Pfr { get; } = new(["pfr"], "application/font-tdpfr");
  ///<summary>gml</summary>
  public static MimeEntry Gml { get; } = new(["gml"], "application/gml+xml");
  ///<summary>gpx</summary>
  public static MimeEntry Gpx { get; } = new(["gpx"], "application/gpx+xml");
  ///<summary>gxf</summary>
  public static MimeEntry Gxf { get; } = new(["gxf"], "application/gxf");
  ///<summary>stk</summary>
  public static MimeEntry Stk { get; } = new(["stk"], "application/hyperstudio");
  ///<summary>ink</summary>
  public static MimeEntry Ink { get; } = new(["ink", "inkml"], "application/inkml+xml");
  ///<summary>inkml</summary>
  public static MimeEntry Inkml { get; } = new(["ink", "inkml"], "application/inkml+xml");
  ///<summary>ipfix</summary>
  public static MimeEntry Ipfix { get; } = new(["ipfix"], "application/ipfix");
  ///<summary>jar</summary>
  public static MimeEntry Jar { get; } = new(["jar", "war", "ear"], "application/java-archive");
  ///<summary>ser</summary>
  public static MimeEntry Ser { get; } = new(["ser"], "application/java-serialized-object");
  ///<summary>class</summary>
  public static MimeEntry Class { get; } = new(["class"], "application/java-vm");
  ///<summary>js</summary>
  public static MimeEntry Js { get; } = new(["js"], "application/javascript");
  ///<summary>json</summary>
  public static MimeEntry Json { get; } = new(["json", "map"], "application/json");
  ///<summary>jsonml</summary>
  public static MimeEntry Jsonml { get; } = new(["jsonml"], "application/jsonml+json");
  ///<summary>lostxml</summary>
  public static MimeEntry Lostxml { get; } = new(["lostxml"], "application/lost+xml");
  ///<summary>hqx</summary>
  public static MimeEntry Hqx { get; } = new(["hqx"], "application/mac-binhex40");
  ///<summary>cpt</summary>
  public static MimeEntry Cpt { get; } = new(["cpt"], "application/mac-compactpro");
  ///<summary>mads</summary>
  public static MimeEntry Mads { get; } = new(["mads"], "application/mads+xml");
  ///<summary>mrc</summary>
  public static MimeEntry Mrc { get; } = new(["mrc"], "application/marc");
  ///<summary>mrcx</summary>
  public static MimeEntry Mrcx { get; } = new(["mrcx"], "application/marcxml+xml");
  ///<summary>ma</summary>
  public static MimeEntry Ma { get; } = new(["ma", "nb", "mb"], "application/mathematica");
  ///<summary>nb</summary>
  public static MimeEntry Nb { get; } = new(["ma", "nb", "mb"], "application/mathematica");
  ///<summary>mb</summary>
  public static MimeEntry Mb { get; } = new(["ma", "nb", "mb"], "application/mathematica");
  ///<summary>mathml</summary>
  public static MimeEntry Mathml { get; } = new(["mathml"], "application/mathml+xml");
  ///<summary>mbox</summary>
  public static MimeEntry Mbox { get; } = new(["mbox"], "application/mbox");
  ///<summary>mscml</summary>
  public static MimeEntry Mscml { get; } = new(["mscml"], "application/mediaservercontrol+xml");
  ///<summary>metalink</summary>
  public static MimeEntry Metalink { get; } = new(["metalink"], "application/metalink+xml");
  ///<summary>meta4</summary>
  public static MimeEntry Meta4 { get; } = new(["meta4"], "application/metalink4+xml");
  ///<summary>mets</summary>
  public static MimeEntry Mets { get; } = new(["mets"], "application/mets+xml");
  ///<summary>mods</summary>
  public static MimeEntry Mods { get; } = new(["mods"], "application/mods+xml");
  ///<summary>m21</summary>
  public static MimeEntry M21 { get; } = new(["m21", "mp21"], "application/mp21");
  ///<summary>mp21</summary>
  public static MimeEntry Mp21 { get; } = new(["m21", "mp21"], "application/mp21");
  ///<summary>mp4s</summary>
  public static MimeEntry Mp4s { get; } = new(["mp4", "mpg4", "mp4s", "m4p"], "application/mp4");
  ///<summary>doc</summary>
  public static MimeEntry Doc { get; } = new(["doc", "dot"], "application/msword");
  ///<summary>dot</summary>
  public static MimeEntry Dot { get; } = new(["doc", "dot"], "application/msword");
  ///<summary>mxf</summary>
  public static MimeEntry Mxf { get; } = new(["mxf"], "application/mxf");
  ///<summary>bin</summary>
  public static MimeEntry Bin { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>dms</summary>
  public static MimeEntry Dms { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>lrf</summary>
  public static MimeEntry Lrf { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>mar</summary>
  public static MimeEntry Mar { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>so</summary>
  public static MimeEntry So { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>dist</summary>
  public static MimeEntry Dist { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>distz</summary>
  public static MimeEntry Distz { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>pkg</summary>
  public static MimeEntry Pkg { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>bpk</summary>
  public static MimeEntry Bpk { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>dump</summary>
  public static MimeEntry Dump { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>elc</summary>
  public static MimeEntry Elc { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>deploy</summary>
  public static MimeEntry Deploy { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>oda</summary>
  public static MimeEntry Oda { get; } = new(["oda"], "application/oda");
  ///<summary>opf</summary>
  public static MimeEntry Opf { get; } = new(["opf"], "application/oebps-package+xml");
  ///<summary>ogx</summary>
  public static MimeEntry Ogx { get; } = new(["ogx"], "application/ogg");
  ///<summary>omdoc</summary>
  public static MimeEntry Omdoc { get; } = new(["omdoc"], "application/omdoc+xml");
  ///<summary>onetoc</summary>
  public static MimeEntry Onetoc { get; } = new(["onetoc", "onetoc2", "onetmp", "onepkg"], "application/onenote");
  ///<summary>onetoc2</summary>
  public static MimeEntry Onetoc2 { get; } = new(["onetoc", "onetoc2", "onetmp", "onepkg"], "application/onenote");
  ///<summary>onetmp</summary>
  public static MimeEntry Onetmp { get; } = new(["onetoc", "onetoc2", "onetmp", "onepkg"], "application/onenote");
  ///<summary>onepkg</summary>
  public static MimeEntry Onepkg { get; } = new(["onetoc", "onetoc2", "onetmp", "onepkg"], "application/onenote");
  ///<summary>oxps</summary>
  public static MimeEntry Oxps { get; } = new(["oxps"], "application/oxps");
  ///<summary>xer</summary>
  public static MimeEntry Xer { get; } = new(["xer"], "application/patch-ops-error+xml");
  ///<summary>pdf</summary>
  public static MimeEntry Pdf { get; } = new(["pdf"], "application/pdf");
  ///<summary>pgp</summary>
  public static MimeEntry Pgp { get; } = new(["pgp"], "application/pgp-encrypted");
  ///<summary>asc</summary>
  public static MimeEntry Asc { get; } = new(["sig"], "application/pgp-signature");
  ///<summary>sig</summary>
  public static MimeEntry Sig { get; } = new(["sig"], "application/pgp-signature");
  ///<summary>prf</summary>
  public static MimeEntry Prf { get; } = new(["prf"], "application/pics-rules");
  ///<summary>p10</summary>
  public static MimeEntry P10 { get; } = new(["p10"], "application/pkcs10");
  ///<summary>p7m</summary>
  public static MimeEntry P7m { get; } = new(["p7m", "p7c"], "application/pkcs7-mime");
  ///<summary>p7c</summary>
  public static MimeEntry P7c { get; } = new(["p7m", "p7c"], "application/pkcs7-mime");
  ///<summary>p7s</summary>
  public static MimeEntry P7s { get; } = new(["p7s"], "application/pkcs7-signature");
  ///<summary>p8</summary>
  public static MimeEntry P8 { get; } = new(["p8"], "application/pkcs8");
  ///<summary>ac</summary>
  public static MimeEntry Ac { get; } = new(["ac"], "application/pkix-attr-cert");
  ///<summary>cer</summary>
  public static MimeEntry Cer { get; } = new(["cer"], "application/pkix-cert");
  ///<summary>crl</summary>
  public static MimeEntry Crl { get; } = new(["crl"], "application/pkix-crl");
  ///<summary>pkipath</summary>
  public static MimeEntry Pkipath { get; } = new(["pkipath"], "application/pkix-pkipath");
  ///<summary>pki</summary>
  public static MimeEntry Pki { get; } = new(["pki"], "application/pkixcmp");
  ///<summary>pls</summary>
  public static MimeEntry Pls { get; } = new(["pls"], "application/pls+xml");
  ///<summary>ai</summary>
  public static MimeEntry Ai { get; } = new(["ai", "eps", "ps"], "application/postscript");
  ///<summary>eps</summary>
  public static MimeEntry Eps { get; } = new(["ai", "eps", "ps"], "application/postscript");
  ///<summary>ps</summary>
  public static MimeEntry Ps { get; } = new(["ai", "eps", "ps"], "application/postscript");
  ///<summary>cww</summary>
  public static MimeEntry Cww { get; } = new(["cww"], "application/prs.cww");
  ///<summary>pskcxml</summary>
  public static MimeEntry Pskcxml { get; } = new(["pskcxml"], "application/pskc+xml");
  ///<summary>rdf</summary>
  public static MimeEntry Rdf { get; } = new(["rdf", "owl"], "application/rdf+xml");
  ///<summary>rif</summary>
  public static MimeEntry Rif { get; } = new(["rif"], "application/reginfo+xml");
  ///<summary>rnc</summary>
  public static MimeEntry Rnc { get; } = new(["rnc"], "application/relax-ng-compact-syntax");
  ///<summary>rl</summary>
  public static MimeEntry Rl { get; } = new(["rl"], "application/resource-lists+xml");
  ///<summary>rld</summary>
  public static MimeEntry Rld { get; } = new(["rld"], "application/resource-lists-diff+xml");
  ///<summary>rs</summary>
  public static MimeEntry Rs { get; } = new(["rs"], "application/rls-services+xml");
  ///<summary>gbr</summary>
  public static MimeEntry Gbr { get; } = new(["gbr"], "application/rpki-ghostbusters");
  ///<summary>mft</summary>
  public static MimeEntry Mft { get; } = new(["mft"], "application/rpki-manifest");
  ///<summary>roa</summary>
  public static MimeEntry Roa { get; } = new(["roa"], "application/rpki-roa");
  ///<summary>rsd</summary>
  public static MimeEntry Rsd { get; } = new(["rsd"], "application/rsd+xml");
  ///<summary>rss</summary>
  public static MimeEntry Rss { get; } = new(["rss"], "application/rss+xml");
  ///<summary>rtf</summary>
  public static MimeEntry Rtf { get; } = new(["rtf"], "application/rtf");
  ///<summary>sbml</summary>
  public static MimeEntry Sbml { get; } = new(["sbml"], "application/sbml+xml");
  ///<summary>scq</summary>
  public static MimeEntry Scq { get; } = new(["scq"], "application/scvp-cv-request");
  ///<summary>scs</summary>
  public static MimeEntry Scs { get; } = new(["scs"], "application/scvp-cv-response");
  ///<summary>spq</summary>
  public static MimeEntry Spq { get; } = new(["spq"], "application/scvp-vp-request");
  ///<summary>spp</summary>
  public static MimeEntry Spp { get; } = new(["spp"], "application/scvp-vp-response");
  ///<summary>sdp</summary>
  public static MimeEntry Sdp { get; } = new(["sdp"], "application/sdp");
  ///<summary>setpay</summary>
  public static MimeEntry Setpay { get; } = new(["setpay"], "application/set-payment-initiation");
  ///<summary>setreg</summary>
  public static MimeEntry Setreg { get; } = new(["setreg"], "application/set-registration-initiation");
  ///<summary>shf</summary>
  public static MimeEntry Shf { get; } = new(["shf"], "application/shf+xml");
  ///<summary>smi</summary>
  public static MimeEntry Smi { get; } = new(["smi", "smil"], "application/smil+xml");
  ///<summary>smil</summary>
  public static MimeEntry Smil { get; } = new(["smi", "smil"], "application/smil+xml");
  ///<summary>rq</summary>
  public static MimeEntry Rq { get; } = new(["rq"], "application/sparql-query");
  ///<summary>srx</summary>
  public static MimeEntry Srx { get; } = new(["srx"], "application/sparql-results+xml");
  ///<summary>gram</summary>
  public static MimeEntry Gram { get; } = new(["gram"], "application/srgs");
  ///<summary>grxml</summary>
  public static MimeEntry Grxml { get; } = new(["grxml"], "application/srgs+xml");
  ///<summary>sru</summary>
  public static MimeEntry Sru { get; } = new(["sru"], "application/sru+xml");
  ///<summary>ssdl</summary>
  public static MimeEntry Ssdl { get; } = new(["ssdl"], "application/ssdl+xml");
  ///<summary>ssml</summary>
  public static MimeEntry Ssml { get; } = new(["ssml"], "application/ssml+xml");
  ///<summary>tei</summary>
  public static MimeEntry Tei { get; } = new(["tei", "teicorpus"], "application/tei+xml");
  ///<summary>teicorpus</summary>
  public static MimeEntry Teicorpus { get; } = new(["tei", "teicorpus"], "application/tei+xml");
  ///<summary>tfi</summary>
  public static MimeEntry Tfi { get; } = new(["tfi"], "application/thraud+xml");
  ///<summary>tsd</summary>
  public static MimeEntry Tsd { get; } = new(["tsd"], "application/timestamped-data");
  ///<summary>plb</summary>
  public static MimeEntry Plb { get; } = new(["plb"], "application/vnd.3gpp.pic-bw-large");
  ///<summary>psb</summary>
  public static MimeEntry Psb { get; } = new(["psb"], "application/vnd.3gpp.pic-bw-small");
  ///<summary>pvb</summary>
  public static MimeEntry Pvb { get; } = new(["pvb"], "application/vnd.3gpp.pic-bw-var");
  ///<summary>tcap</summary>
  public static MimeEntry Tcap { get; } = new(["tcap"], "application/vnd.3gpp2.tcap");
  ///<summary>pwn</summary>
  public static MimeEntry Pwn { get; } = new(["pwn"], "application/vnd.3m.post-it-notes");
  ///<summary>aso</summary>
  public static MimeEntry Aso { get; } = new(["aso"], "application/vnd.accpac.simply.aso");
  ///<summary>imp</summary>
  public static MimeEntry Imp { get; } = new(["imp"], "application/vnd.accpac.simply.imp");
  ///<summary>acu</summary>
  public static MimeEntry Acu { get; } = new(["acu"], "application/vnd.acucobol");
  ///<summary>atc</summary>
  public static MimeEntry Atc { get; } = new(["atc", "acutc"], "application/vnd.acucorp");
  ///<summary>acutc</summary>
  public static MimeEntry Acutc { get; } = new(["atc", "acutc"], "application/vnd.acucorp");
  ///<summary>air</summary>
  public static MimeEntry Air { get; } = new(["air"], "application/vnd.adobe.air-application-installer-package+zip");
  ///<summary>fcdt</summary>
  public static MimeEntry Fcdt { get; } = new(["fcdt"], "application/vnd.adobe.formscentral.fcdt");
  ///<summary>fxp</summary>
  public static MimeEntry Fxp { get; } = new(["fxp", "fxpl"], "application/vnd.adobe.fxp");
  ///<summary>fxpl</summary>
  public static MimeEntry Fxpl { get; } = new(["fxp", "fxpl"], "application/vnd.adobe.fxp");
  ///<summary>xdp</summary>
  public static MimeEntry Xdp { get; } = new(["xdp"], "application/vnd.adobe.xdp+xml");
  ///<summary>xfdf</summary>
  public static MimeEntry Xfdf { get; } = new(["xfdf"], "application/vnd.adobe.xfdf");
  ///<summary>ahead</summary>
  public static MimeEntry Ahead { get; } = new(["ahead"], "application/vnd.ahead.space");
  ///<summary>azf</summary>
  public static MimeEntry Azf { get; } = new(["azf"], "application/vnd.airzip.filesecure.azf");
  ///<summary>azs</summary>
  public static MimeEntry Azs { get; } = new(["azs"], "application/vnd.airzip.filesecure.azs");
  ///<summary>azw</summary>
  public static MimeEntry Azw { get; } = new(["azw"], "application/vnd.amazon.ebook");
  ///<summary>acc</summary>
  public static MimeEntry Acc { get; } = new(["acc"], "application/vnd.americandynamics.acc");
  ///<summary>ami</summary>
  public static MimeEntry Ami { get; } = new(["ami"], "application/vnd.amiga.ami");
  ///<summary>apk</summary>
  public static MimeEntry Apk { get; } = new(["apk"], "application/vnd.android.package-archive");
  ///<summary>cii</summary>
  public static MimeEntry Cii { get; } = new(["cii"], "application/vnd.anser-web-certificate-issue-initiation");
  ///<summary>fti</summary>
  public static MimeEntry Fti { get; } = new(["fti"], "application/vnd.anser-web-funds-transfer-initiation");
  ///<summary>atx</summary>
  public static MimeEntry Atx { get; } = new(["atx"], "application/vnd.antix.game-component");
  ///<summary>mpkg</summary>
  public static MimeEntry Mpkg { get; } = new(["mpkg"], "application/vnd.apple.installer+xml");
  ///<summary>m3u8</summary>
  public static MimeEntry M3u8 { get; } = new(["m3u8"], "application/vnd.apple.mpegurl");
  ///<summary>swi</summary>
  public static MimeEntry Swi { get; } = new(["swi"], "application/vnd.aristanetworks.swi");
  ///<summary>iota</summary>
  public static MimeEntry Iota { get; } = new(["iota"], "application/vnd.astraea-software.iota");
  ///<summary>aep</summary>
  public static MimeEntry Aep { get; } = new(["aep"], "application/vnd.audiograph");
  ///<summary>mpm</summary>
  public static MimeEntry Mpm { get; } = new(["mpm"], "application/vnd.blueice.multipass");
  ///<summary>bmi</summary>
  public static MimeEntry Bmi { get; } = new(["bmi"], "application/vnd.bmi");
  ///<summary>rep</summary>
  public static MimeEntry Rep { get; } = new(["rep"], "application/vnd.businessobjects");
  ///<summary>cdxml</summary>
  public static MimeEntry Cdxml { get; } = new(["cdxml"], "application/vnd.chemdraw+xml");
  ///<summary>mmd</summary>
  public static MimeEntry Mmd { get; } = new(["mmd"], "application/vnd.chipnuts.karaoke-mmd");
  ///<summary>cdy</summary>
  public static MimeEntry Cdy { get; } = new(["cdy"], "application/vnd.cinderella");
  ///<summary>cla</summary>
  public static MimeEntry Cla { get; } = new(["cla"], "application/vnd.claymore");
  ///<summary>rp9</summary>
  public static MimeEntry Rp9 { get; } = new(["rp9"], "application/vnd.cloanto.rp9");
  ///<summary>c4g</summary>
  public static MimeEntry C4g { get; } = new(["c4g", "c4d", "c4f", "c4p", "c4u"], "application/vnd.clonk.c4group");
  ///<summary>c4d</summary>
  public static MimeEntry C4d { get; } = new(["c4g", "c4d", "c4f", "c4p", "c4u"], "application/vnd.clonk.c4group");
  ///<summary>c4f</summary>
  public static MimeEntry C4f { get; } = new(["c4g", "c4d", "c4f", "c4p", "c4u"], "application/vnd.clonk.c4group");
  ///<summary>c4p</summary>
  public static MimeEntry C4p { get; } = new(["c4g", "c4d", "c4f", "c4p", "c4u"], "application/vnd.clonk.c4group");
  ///<summary>c4u</summary>
  public static MimeEntry C4u { get; } = new(["c4g", "c4d", "c4f", "c4p", "c4u"], "application/vnd.clonk.c4group");
  ///<summary>c11amc</summary>
  public static MimeEntry C11amc { get; } = new(["c11amc"], "application/vnd.cluetrust.cartomobile-config");
  ///<summary>c11amz</summary>
  public static MimeEntry C11amz { get; } = new(["c11amz"], "application/vnd.cluetrust.cartomobile-config-pkg");
  ///<summary>csp</summary>
  public static MimeEntry Csp { get; } = new(["csp"], "application/vnd.commonspace");
  ///<summary>cdbcmsg</summary>
  public static MimeEntry Cdbcmsg { get; } = new(["cdbcmsg"], "application/vnd.contact.cmsg");
  ///<summary>cmc</summary>
  public static MimeEntry Cmc { get; } = new(["cmc"], "application/vnd.cosmocaller");
  ///<summary>clkx</summary>
  public static MimeEntry Clkx { get; } = new(["clkx"], "application/vnd.crick.clicker");
  ///<summary>clkk</summary>
  public static MimeEntry Clkk { get; } = new(["clkk"], "application/vnd.crick.clicker.keyboard");
  ///<summary>clkp</summary>
  public static MimeEntry Clkp { get; } = new(["clkp"], "application/vnd.crick.clicker.palette");
  ///<summary>clkt</summary>
  public static MimeEntry Clkt { get; } = new(["clkt"], "application/vnd.crick.clicker.template");
  ///<summary>clkw</summary>
  public static MimeEntry Clkw { get; } = new(["clkw"], "application/vnd.crick.clicker.wordbank");
  ///<summary>wbs</summary>
  public static MimeEntry Wbs { get; } = new(["wbs"], "application/vnd.criticaltools.wbs+xml");
  ///<summary>pml</summary>
  public static MimeEntry Pml { get; } = new(["pml"], "application/vnd.ctc-posml");
  ///<summary>ppd</summary>
  public static MimeEntry Ppd { get; } = new(["ppd"], "application/vnd.cups-ppd");
  ///<summary>car</summary>
  public static MimeEntry Car { get; } = new(["car"], "application/vnd.curl.car");
  ///<summary>pcurl</summary>
  public static MimeEntry Pcurl { get; } = new(["pcurl"], "application/vnd.curl.pcurl");
  ///<summary>dart</summary>
  public static MimeEntry Dart { get; } = new(["dart"], "application/vnd.dart");
  ///<summary>rdz</summary>
  public static MimeEntry Rdz { get; } = new(["rdz"], "application/vnd.data-vision.rdz");
  ///<summary>uvf</summary>
  public static MimeEntry Uvf { get; } = new(["uvf", "uvvf", "uvd", "uvvd"], "application/vnd.dece.data");
  ///<summary>uvvf</summary>
  public static MimeEntry Uvvf { get; } = new(["uvf", "uvvf", "uvd", "uvvd"], "application/vnd.dece.data");
  ///<summary>uvd</summary>
  public static MimeEntry Uvd { get; } = new(["uvf", "uvvf", "uvd", "uvvd"], "application/vnd.dece.data");
  ///<summary>uvvd</summary>
  public static MimeEntry Uvvd { get; } = new(["uvf", "uvvf", "uvd", "uvvd"], "application/vnd.dece.data");
  ///<summary>uvt</summary>
  public static MimeEntry Uvt { get; } = new(["uvt", "uvvt"], "application/vnd.dece.ttml+xml");
  ///<summary>uvvt</summary>
  public static MimeEntry Uvvt { get; } = new(["uvt", "uvvt"], "application/vnd.dece.ttml+xml");
  ///<summary>uvx</summary>
  public static MimeEntry Uvx { get; } = new(["uvx", "uvvx"], "application/vnd.dece.unspecified");
  ///<summary>uvvx</summary>
  public static MimeEntry Uvvx { get; } = new(["uvx", "uvvx"], "application/vnd.dece.unspecified");
  ///<summary>uvz</summary>
  public static MimeEntry Uvz { get; } = new(["uvz", "uvvz"], "application/vnd.dece.zip");
  ///<summary>uvvz</summary>
  public static MimeEntry Uvvz { get; } = new(["uvz", "uvvz"], "application/vnd.dece.zip");
  ///<summary>fe_launch</summary>
  public static MimeEntry Felaunch { get; } = new(["fe_launch"], "application/vnd.denovo.fcselayout-link");
  ///<summary>dna</summary>
  public static MimeEntry Dna { get; } = new(["dna"], "application/vnd.dna");
  ///<summary>mlp</summary>
  public static MimeEntry Mlp { get; } = new(["mlp"], "application/vnd.dolby.mlp");
  ///<summary>dpg</summary>
  public static MimeEntry Dpg { get; } = new(["dpg"], "application/vnd.dpgraph");
  ///<summary>dfac</summary>
  public static MimeEntry Dfac { get; } = new(["dfac"], "application/vnd.dreamfactory");
  ///<summary>kpxx</summary>
  public static MimeEntry Kpxx { get; } = new(["kpxx"], "application/vnd.ds-keypoint");
  ///<summary>ait</summary>
  public static MimeEntry Ait { get; } = new(["ait"], "application/vnd.dvb.ait");
  ///<summary>svc</summary>
  public static MimeEntry Svc { get; } = new(["svc"], "application/vnd.dvb.service");
  ///<summary>geo</summary>
  public static MimeEntry Geo { get; } = new(["geo"], "application/vnd.dynageo");
  ///<summary>mag</summary>
  public static MimeEntry Mag { get; } = new(["mag"], "application/vnd.ecowin.chart");
  ///<summary>nml</summary>
  public static MimeEntry Nml { get; } = new(["nml"], "application/vnd.enliven");
  ///<summary>esf</summary>
  public static MimeEntry Esf { get; } = new(["esf"], "application/vnd.epson.esf");
  ///<summary>msf</summary>
  public static MimeEntry Msf { get; } = new(["msf"], "application/vnd.epson.msf");
  ///<summary>qam</summary>
  public static MimeEntry Qam { get; } = new(["qam"], "application/vnd.epson.quickanime");
  ///<summary>slt</summary>
  public static MimeEntry Slt { get; } = new(["slt"], "application/vnd.epson.salt");
  ///<summary>ssf</summary>
  public static MimeEntry Ssf { get; } = new(["ssf"], "application/vnd.epson.ssf");
  ///<summary>es3</summary>
  public static MimeEntry Es3 { get; } = new(["es3", "et3"], "application/vnd.eszigno3+xml");
  ///<summary>et3</summary>
  public static MimeEntry Et3 { get; } = new(["es3", "et3"], "application/vnd.eszigno3+xml");
  ///<summary>ez2</summary>
  public static MimeEntry Ez2 { get; } = new(["ez2"], "application/vnd.ezpix-album");
  ///<summary>ez3</summary>
  public static MimeEntry Ez3 { get; } = new(["ez3"], "application/vnd.ezpix-package");
  ///<summary>fdf</summary>
  public static MimeEntry Fdf { get; } = new(["fdf"], "application/vnd.fdf");
  ///<summary>mseed</summary>
  public static MimeEntry Mseed { get; } = new(["mseed"], "application/vnd.fdsn.mseed");
  ///<summary>seed</summary>
  public static MimeEntry Seed { get; } = new(["seed", "dataless"], "application/vnd.fdsn.seed");
  ///<summary>dataless</summary>
  public static MimeEntry Dataless { get; } = new(["seed", "dataless"], "application/vnd.fdsn.seed");
  ///<summary>gph</summary>
  public static MimeEntry Gph { get; } = new(["gph"], "application/vnd.flographit");
  ///<summary>ftc</summary>
  public static MimeEntry Ftc { get; } = new(["ftc"], "application/vnd.fluxtime.clip");
  ///<summary>fm</summary>
  public static MimeEntry Fm { get; } = new(["fm", "frame", "maker", "book"], "application/vnd.framemaker");
  ///<summary>frame</summary>
  public static MimeEntry Frame { get; } = new(["fm", "frame", "maker", "book"], "application/vnd.framemaker");
  ///<summary>maker</summary>
  public static MimeEntry Maker { get; } = new(["fm", "frame", "maker", "book"], "application/vnd.framemaker");
  ///<summary>book</summary>
  public static MimeEntry Book { get; } = new(["fm", "frame", "maker", "book"], "application/vnd.framemaker");
  ///<summary>fnc</summary>
  public static MimeEntry Fnc { get; } = new(["fnc"], "application/vnd.frogans.fnc");
  ///<summary>ltf</summary>
  public static MimeEntry Ltf { get; } = new(["ltf"], "application/vnd.frogans.ltf");
  ///<summary>fsc</summary>
  public static MimeEntry Fsc { get; } = new(["fsc"], "application/vnd.fsc.weblaunch");
  ///<summary>oas</summary>
  public static MimeEntry Oas { get; } = new(["oas"], "application/vnd.fujitsu.oasys");
  ///<summary>oa2</summary>
  public static MimeEntry Oa2 { get; } = new(["oa2"], "application/vnd.fujitsu.oasys2");
  ///<summary>oa3</summary>
  public static MimeEntry Oa3 { get; } = new(["oa3"], "application/vnd.fujitsu.oasys3");
  ///<summary>fg5</summary>
  public static MimeEntry Fg5 { get; } = new(["fg5"], "application/vnd.fujitsu.oasysgp");
  ///<summary>bh2</summary>
  public static MimeEntry Bh2 { get; } = new(["bh2"], "application/vnd.fujitsu.oasysprs");
  ///<summary>ddd</summary>
  public static MimeEntry Ddd { get; } = new(["ddd"], "application/vnd.fujixerox.ddd");
  ///<summary>xdw</summary>
  public static MimeEntry Xdw { get; } = new(["xdw"], "application/vnd.fujixerox.docuworks");
  ///<summary>xbd</summary>
  public static MimeEntry Xbd { get; } = new(["xbd"], "application/vnd.fujixerox.docuworks.binder");
  ///<summary>fzs</summary>
  public static MimeEntry Fzs { get; } = new(["fzs"], "application/vnd.fuzzysheet");
  ///<summary>txd</summary>
  public static MimeEntry Txd { get; } = new(["txd"], "application/vnd.genomatix.tuxedo");
  ///<summary>ggb</summary>
  public static MimeEntry Ggb { get; } = new(["ggb"], "application/vnd.geogebra.file");
  ///<summary>ggt</summary>
  public static MimeEntry Ggt { get; } = new(["ggt"], "application/vnd.geogebra.tool");
  ///<summary>gex</summary>
  public static MimeEntry Gex { get; } = new(["gex", "gre"], "application/vnd.geometry-explorer");
  ///<summary>gre</summary>
  public static MimeEntry Gre { get; } = new(["gex", "gre"], "application/vnd.geometry-explorer");
  ///<summary>gxt</summary>
  public static MimeEntry Gxt { get; } = new(["gxt"], "application/vnd.geonext");
  ///<summary>g2w</summary>
  public static MimeEntry G2w { get; } = new(["g2w"], "application/vnd.geoplan");
  ///<summary>g3w</summary>
  public static MimeEntry G3w { get; } = new(["g3w"], "application/vnd.geospace");
  ///<summary>gmx</summary>
  public static MimeEntry Gmx { get; } = new(["gmx"], "application/vnd.gmx");
  ///<summary>kml</summary>
  public static MimeEntry Kml { get; } = new(["kml"], "application/vnd.google-earth.kml+xml");
  ///<summary>kmz</summary>
  public static MimeEntry Kmz { get; } = new(["kmz"], "application/vnd.google-earth.kmz");
  ///<summary>gqf</summary>
  public static MimeEntry Gqf { get; } = new(["gqf", "gqs"], "application/vnd.grafeq");
  ///<summary>gqs</summary>
  public static MimeEntry Gqs { get; } = new(["gqf", "gqs"], "application/vnd.grafeq");
  ///<summary>gac</summary>
  public static MimeEntry Gac { get; } = new(["gac"], "application/vnd.groove-account");
  ///<summary>ghf</summary>
  public static MimeEntry Ghf { get; } = new(["ghf"], "application/vnd.groove-help");
  ///<summary>gim</summary>
  public static MimeEntry Gim { get; } = new(["gim"], "application/vnd.groove-identity-message");
  ///<summary>grv</summary>
  public static MimeEntry Grv { get; } = new(["grv"], "application/vnd.groove-injector");
  ///<summary>gtm</summary>
  public static MimeEntry Gtm { get; } = new(["gtm"], "application/vnd.groove-tool-message");
  ///<summary>tpl</summary>
  public static MimeEntry Tpl { get; } = new(["tpl"], "application/vnd.groove-tool-template");
  ///<summary>vcg</summary>
  public static MimeEntry Vcg { get; } = new(["vcg"], "application/vnd.groove-vcard");
  ///<summary>hal</summary>
  public static MimeEntry Hal { get; } = new(["hal"], "application/vnd.hal+xml");
  ///<summary>zmm</summary>
  public static MimeEntry Zmm { get; } = new(["zmm"], "application/vnd.handheld-entertainment+xml");
  ///<summary>hbci</summary>
  public static MimeEntry Hbci { get; } = new(["hbci"], "application/vnd.hbci");
  ///<summary>les</summary>
  public static MimeEntry Les { get; } = new(["les"], "application/vnd.hhe.lesson-player");
  ///<summary>hpgl</summary>
  public static MimeEntry Hpgl { get; } = new(["hpgl"], "application/vnd.hp-hpgl");
  ///<summary>hpid</summary>
  public static MimeEntry Hpid { get; } = new(["hpid"], "application/vnd.hp-hpid");
  ///<summary>hps</summary>
  public static MimeEntry Hps { get; } = new(["hps"], "application/vnd.hp-hps");
  ///<summary>jlt</summary>
  public static MimeEntry Jlt { get; } = new(["jlt"], "application/vnd.hp-jlyt");
  ///<summary>pcl</summary>
  public static MimeEntry Pcl { get; } = new(["pcl"], "application/vnd.hp-pcl");
  ///<summary>pclxl</summary>
  public static MimeEntry Pclxl { get; } = new(["pclxl"], "application/vnd.hp-pclxl");
  ///<summary>sfd-hdstx</summary>
  public static MimeEntry Sfdhdstx { get; } = new(["sfd-hdstx"], "application/vnd.hydrostatix.sof-data");
  ///<summary>mpy</summary>
  public static MimeEntry Mpy { get; } = new(["mpy"], "application/vnd.ibm.minipay");
  ///<summary>afp</summary>
  public static MimeEntry Afp { get; } = new(["afp", "listafp", "list3820"], "application/vnd.ibm.modcap");
  ///<summary>listafp</summary>
  public static MimeEntry Listafp { get; } = new(["afp", "listafp", "list3820"], "application/vnd.ibm.modcap");
  ///<summary>list3820</summary>
  public static MimeEntry List3820 { get; } = new(["afp", "listafp", "list3820"], "application/vnd.ibm.modcap");
  ///<summary>irm</summary>
  public static MimeEntry Irm { get; } = new(["irm"], "application/vnd.ibm.rights-management");
  ///<summary>sc</summary>
  public static MimeEntry Sc { get; } = new(["sc"], "application/vnd.ibm.secure-container");
  ///<summary>icc</summary>
  public static MimeEntry Icc { get; } = new(["icc", "icm"], "application/vnd.iccprofile");
  ///<summary>icm</summary>
  public static MimeEntry Icm { get; } = new(["icc", "icm"], "application/vnd.iccprofile");
  ///<summary>igl</summary>
  public static MimeEntry Igl { get; } = new(["igl"], "application/vnd.igloader");
  ///<summary>ivp</summary>
  public static MimeEntry Ivp { get; } = new(["ivp"], "application/vnd.immervision-ivp");
  ///<summary>ivu</summary>
  public static MimeEntry Ivu { get; } = new(["ivu"], "application/vnd.immervision-ivu");
  ///<summary>igm</summary>
  public static MimeEntry Igm { get; } = new(["igm"], "application/vnd.insors.igm");
  ///<summary>xpw</summary>
  public static MimeEntry Xpw { get; } = new(["xpw", "xpx"], "application/vnd.intercon.formnet");
  ///<summary>xpx</summary>
  public static MimeEntry Xpx { get; } = new(["xpw", "xpx"], "application/vnd.intercon.formnet");
  ///<summary>i2g</summary>
  public static MimeEntry I2g { get; } = new(["i2g"], "application/vnd.intergeo");
  ///<summary>qbo</summary>
  public static MimeEntry Qbo { get; } = new(["qbo"], "application/vnd.intu.qbo");
  ///<summary>qfx</summary>
  public static MimeEntry Qfx { get; } = new(["qfx"], "application/vnd.intu.qfx");
  ///<summary>rcprofile</summary>
  public static MimeEntry Rcprofile { get; } = new(["rcprofile"], "application/vnd.ipunplugged.rcprofile");
  ///<summary>irp</summary>
  public static MimeEntry Irp { get; } = new(["irp"], "application/vnd.irepository.package+xml");
  ///<summary>xpr</summary>
  public static MimeEntry Xpr { get; } = new(["xpr"], "application/vnd.is-xpr");
  ///<summary>fcs</summary>
  public static MimeEntry Fcs { get; } = new(["fcs"], "application/vnd.isac.fcs");
  ///<summary>jam</summary>
  public static MimeEntry Jam { get; } = new(["jam"], "application/vnd.jam");
  ///<summary>rms</summary>
  public static MimeEntry Rms { get; } = new(["rms"], "application/vnd.jcp.javame.midlet-rms");
  ///<summary>jisp</summary>
  public static MimeEntry Jisp { get; } = new(["jisp"], "application/vnd.jisp");
  ///<summary>joda</summary>
  public static MimeEntry Joda { get; } = new(["joda"], "application/vnd.joost.joda-archive");
  ///<summary>ktz</summary>
  public static MimeEntry Ktz { get; } = new(["ktz", "ktr"], "application/vnd.kahootz");
  ///<summary>ktr</summary>
  public static MimeEntry Ktr { get; } = new(["ktz", "ktr"], "application/vnd.kahootz");
  ///<summary>karbon</summary>
  public static MimeEntry Karbon { get; } = new(["karbon"], "application/vnd.kde.karbon");
  ///<summary>chrt</summary>
  public static MimeEntry Chrt { get; } = new(["chrt"], "application/vnd.kde.kchart");
  ///<summary>kfo</summary>
  public static MimeEntry Kfo { get; } = new(["kfo"], "application/vnd.kde.kformula");
  ///<summary>flw</summary>
  public static MimeEntry Flw { get; } = new(["flw"], "application/vnd.kde.kivio");
  ///<summary>kon</summary>
  public static MimeEntry Kon { get; } = new(["kon"], "application/vnd.kde.kontour");
  ///<summary>kpr</summary>
  public static MimeEntry Kpr { get; } = new(["kpr", "kpt"], "application/vnd.kde.kpresenter");
  ///<summary>kpt</summary>
  public static MimeEntry Kpt { get; } = new(["kpr", "kpt"], "application/vnd.kde.kpresenter");
  ///<summary>ksp</summary>
  public static MimeEntry Ksp { get; } = new(["ksp"], "application/vnd.kde.kspread");
  ///<summary>kwd</summary>
  public static MimeEntry Kwd { get; } = new(["kwd", "kwt"], "application/vnd.kde.kword");
  ///<summary>kwt</summary>
  public static MimeEntry Kwt { get; } = new(["kwd", "kwt"], "application/vnd.kde.kword");
  ///<summary>htke</summary>
  public static MimeEntry Htke { get; } = new(["htke"], "application/vnd.kenameaapp");
  ///<summary>kia</summary>
  public static MimeEntry Kia { get; } = new(["kia"], "application/vnd.kidspiration");
  ///<summary>kne</summary>
  public static MimeEntry Kne { get; } = new(["kne", "knp"], "application/vnd.kinar");
  ///<summary>knp</summary>
  public static MimeEntry Knp { get; } = new(["kne", "knp"], "application/vnd.kinar");
  ///<summary>skp</summary>
  public static MimeEntry Skp { get; } = new(["skp", "skd", "skt", "skm"], "application/vnd.koan");
  ///<summary>skd</summary>
  public static MimeEntry Skd { get; } = new(["skp", "skd", "skt", "skm"], "application/vnd.koan");
  ///<summary>skt</summary>
  public static MimeEntry Skt { get; } = new(["skp", "skd", "skt", "skm"], "application/vnd.koan");
  ///<summary>skm</summary>
  public static MimeEntry Skm { get; } = new(["skp", "skd", "skt", "skm"], "application/vnd.koan");
  ///<summary>sse</summary>
  public static MimeEntry Sse { get; } = new(["sse"], "application/vnd.kodak-descriptor");
  ///<summary>lasxml</summary>
  public static MimeEntry Lasxml { get; } = new(["lasxml"], "application/vnd.las.las+xml");
  ///<summary>lbd</summary>
  public static MimeEntry Lbd { get; } = new(["lbd"], "application/vnd.llamagraphics.life-balance.desktop");
  ///<summary>lbe</summary>
  public static MimeEntry Lbe { get; } = new(["lbe"], "application/vnd.llamagraphics.life-balance.exchange+xml");
  ///<summary>123</summary>
  public static MimeEntry _123 { get; } = new(["123"], "application/vnd.lotus-1-2-3");
  ///<summary>apr</summary>
  public static MimeEntry Apr { get; } = new(["apr"], "application/vnd.lotus-approach");
  ///<summary>pre</summary>
  public static MimeEntry Pre { get; } = new(["pre"], "application/vnd.lotus-freelance");
  ///<summary>nsf</summary>
  public static MimeEntry Nsf { get; } = new(["nsf"], "application/vnd.lotus-notes");
  ///<summary>org</summary>
  public static MimeEntry Org { get; } = new(["org"], "application/vnd.lotus-organizer");
  ///<summary>scm</summary>
  public static MimeEntry Scm { get; } = new(["scm"], "application/vnd.lotus-screencam");
  ///<summary>lwp</summary>
  public static MimeEntry Lwp { get; } = new(["lwp"], "application/vnd.lotus-wordpro");
  ///<summary>portpkg</summary>
  public static MimeEntry Portpkg { get; } = new(["portpkg"], "application/vnd.macports.portpkg");
  ///<summary>mcd</summary>
  public static MimeEntry Mcd { get; } = new(["mcd"], "application/vnd.mcd");
  ///<summary>mc1</summary>
  public static MimeEntry Mc1 { get; } = new(["mc1"], "application/vnd.medcalcdata");
  ///<summary>cdkey</summary>
  public static MimeEntry Cdkey { get; } = new(["cdkey"], "application/vnd.mediastation.cdkey");
  ///<summary>mwf</summary>
  public static MimeEntry Mwf { get; } = new(["mwf"], "application/vnd.mfer");
  ///<summary>mfm</summary>
  public static MimeEntry Mfm { get; } = new(["mfm"], "application/vnd.mfmp");
  ///<summary>flo</summary>
  public static MimeEntry Flo { get; } = new(["flo"], "application/vnd.micrografx.flo");
  ///<summary>igx</summary>
  public static MimeEntry Igx { get; } = new(["igx"], "application/vnd.micrografx.igx");
  ///<summary>mif</summary>
  public static MimeEntry Mif { get; } = new(["mif"], "application/vnd.mif");
  ///<summary>daf</summary>
  public static MimeEntry Daf { get; } = new(["daf"], "application/vnd.mobius.daf");
  ///<summary>dis</summary>
  public static MimeEntry Dis { get; } = new(["dis"], "application/vnd.mobius.dis");
  ///<summary>mbk</summary>
  public static MimeEntry Mbk { get; } = new(["mbk"], "application/vnd.mobius.mbk");
  ///<summary>mqy</summary>
  public static MimeEntry Mqy { get; } = new(["mqy"], "application/vnd.mobius.mqy");
  ///<summary>msl</summary>
  public static MimeEntry Msl { get; } = new(["msl"], "application/vnd.mobius.msl");
  ///<summary>plc</summary>
  public static MimeEntry Plc { get; } = new(["plc"], "application/vnd.mobius.plc");
  ///<summary>txf</summary>
  public static MimeEntry Txf { get; } = new(["txf"], "application/vnd.mobius.txf");
  ///<summary>mpn</summary>
  public static MimeEntry Mpn { get; } = new(["mpn"], "application/vnd.mophun.application");
  ///<summary>mpc</summary>
  public static MimeEntry Mpc { get; } = new(["mpc"], "application/vnd.mophun.certificate");
  ///<summary>xul</summary>
  public static MimeEntry Xul { get; } = new(["xul"], "application/vnd.mozilla.xul+xml");
  ///<summary>cil</summary>
  public static MimeEntry Cil { get; } = new(["cil"], "application/vnd.ms-artgalry");
  ///<summary>cab</summary>
  public static MimeEntry Cab { get; } = new(["cab"], "application/vnd.ms-cab-compressed");
  ///<summary>xls</summary>
  public static MimeEntry Xls { get; } = new(["xls", "xlm", "xla", "xlc", "xlt", "xlw"], "application/vnd.ms-excel");
  ///<summary>xlm</summary>
  public static MimeEntry Xlm { get; } = new(["xls", "xlm", "xla", "xlc", "xlt", "xlw"], "application/vnd.ms-excel");
  ///<summary>xla</summary>
  public static MimeEntry Xla { get; } = new(["xls", "xlm", "xla", "xlc", "xlt", "xlw"], "application/vnd.ms-excel");
  ///<summary>xlc</summary>
  public static MimeEntry Xlc { get; } = new(["xls", "xlm", "xla", "xlc", "xlt", "xlw"], "application/vnd.ms-excel");
  ///<summary>xlt</summary>
  public static MimeEntry Xlt { get; } = new(["xls", "xlm", "xla", "xlc", "xlt", "xlw"], "application/vnd.ms-excel");
  ///<summary>xlw</summary>
  public static MimeEntry Xlw { get; } = new(["xls", "xlm", "xla", "xlc", "xlt", "xlw"], "application/vnd.ms-excel");
  ///<summary>xlam</summary>
  public static MimeEntry Xlam { get; } = new(["xlam"], "application/vnd.ms-excel.addin.macroenabled.12");
  ///<summary>xlsb</summary>
  public static MimeEntry Xlsb { get; } = new(["xlsb"], "application/vnd.ms-excel.sheet.binary.macroenabled.12");
  ///<summary>xlsm</summary>
  public static MimeEntry Xlsm { get; } = new(["xlsm"], "application/vnd.ms-excel.sheet.macroenabled.12");
  ///<summary>xltm</summary>
  public static MimeEntry Xltm { get; } = new(["xltm"], "application/vnd.ms-excel.template.macroenabled.12");
  ///<summary>eot</summary>
  public static MimeEntry Eot { get; } = new(["eot"], "application/vnd.ms-fontobject");
  ///<summary>chm</summary>
  public static MimeEntry Chm { get; } = new(["chm"], "application/vnd.ms-htmlhelp");
  ///<summary>ims</summary>
  public static MimeEntry Ims { get; } = new(["ims"], "application/vnd.ms-ims");
  ///<summary>lrm</summary>
  public static MimeEntry Lrm { get; } = new(["lrm"], "application/vnd.ms-lrm");
  ///<summary>thmx</summary>
  public static MimeEntry Thmx { get; } = new(["thmx"], "application/vnd.ms-officetheme");
  ///<summary>cat</summary>
  public static MimeEntry Cat { get; } = new(["cat"], "application/vnd.ms-pki.seccat");
  ///<summary>stl</summary>
  public static MimeEntry Stl { get; } = new(["stl"], "application/vnd.ms-pki.stl");
  ///<summary>ppt</summary>
  public static MimeEntry Ppt { get; } = new(["ppt", "pps", "pot"], "application/vnd.ms-powerpoint");
  ///<summary>pps</summary>
  public static MimeEntry Pps { get; } = new(["ppt", "pps", "pot"], "application/vnd.ms-powerpoint");
  ///<summary>pot</summary>
  public static MimeEntry Pot { get; } = new(["ppt", "pps", "pot"], "application/vnd.ms-powerpoint");
  ///<summary>ppam</summary>
  public static MimeEntry Ppam { get; } = new(["ppam"], "application/vnd.ms-powerpoint.addin.macroenabled.12");
  ///<summary>pptm</summary>
  public static MimeEntry Pptm { get; } = new(["pptm"], "application/vnd.ms-powerpoint.presentation.macroenabled.12");
  ///<summary>sldm</summary>
  public static MimeEntry Sldm { get; } = new(["sldm"], "application/vnd.ms-powerpoint.slide.macroenabled.12");
  ///<summary>ppsm</summary>
  public static MimeEntry Ppsm { get; } = new(["ppsm"], "application/vnd.ms-powerpoint.slideshow.macroenabled.12");
  ///<summary>potm</summary>
  public static MimeEntry Potm { get; } = new(["potm"], "application/vnd.ms-powerpoint.template.macroenabled.12");
  ///<summary>mpp</summary>
  public static MimeEntry Mpp { get; } = new(["mpt"], "application/vnd.ms-project");
  ///<summary>mpt</summary>
  public static MimeEntry Mpt { get; } = new(["mpt"], "application/vnd.ms-project");
  ///<summary>docm</summary>
  public static MimeEntry Docm { get; } = new(["docm"], "application/vnd.ms-word.document.macroenabled.12");
  ///<summary>dotm</summary>
  public static MimeEntry Dotm { get; } = new(["dotm"], "application/vnd.ms-word.template.macroenabled.12");
  ///<summary>wps</summary>
  public static MimeEntry Wps { get; } = new(["wps", "wks", "wcm", "wdb"], "application/vnd.ms-works");
  ///<summary>wks</summary>
  public static MimeEntry Wks { get; } = new(["wps", "wks", "wcm", "wdb"], "application/vnd.ms-works");
  ///<summary>wcm</summary>
  public static MimeEntry Wcm { get; } = new(["wps", "wks", "wcm", "wdb"], "application/vnd.ms-works");
  ///<summary>wdb</summary>
  public static MimeEntry Wdb { get; } = new(["wps", "wks", "wcm", "wdb"], "application/vnd.ms-works");
  ///<summary>wpl</summary>
  public static MimeEntry Wpl { get; } = new(["wpl"], "application/vnd.ms-wpl");
  ///<summary>xps</summary>
  public static MimeEntry Xps { get; } = new(["xps"], "application/vnd.ms-xpsdocument");
  ///<summary>mseq</summary>
  public static MimeEntry Mseq { get; } = new(["mseq"], "application/vnd.mseq");
  ///<summary>mus</summary>
  public static MimeEntry Mus { get; } = new(["mus"], "application/vnd.musician");
  ///<summary>msty</summary>
  public static MimeEntry Msty { get; } = new(["msty"], "application/vnd.muvee.style");
  ///<summary>taglet</summary>
  public static MimeEntry Taglet { get; } = new(["taglet"], "application/vnd.mynfc");
  ///<summary>nlu</summary>
  public static MimeEntry Nlu { get; } = new(["nlu"], "application/vnd.neurolanguage.nlu");
  ///<summary>ntf</summary>
  public static MimeEntry Ntf { get; } = new(["ntf", "nitf"], "application/vnd.nitf");
  ///<summary>nitf</summary>
  public static MimeEntry Nitf { get; } = new(["ntf", "nitf"], "application/vnd.nitf");
  ///<summary>nnd</summary>
  public static MimeEntry Nnd { get; } = new(["nnd"], "application/vnd.noblenet-directory");
  ///<summary>nns</summary>
  public static MimeEntry Nns { get; } = new(["nns"], "application/vnd.noblenet-sealer");
  ///<summary>nnw</summary>
  public static MimeEntry Nnw { get; } = new(["nnw"], "application/vnd.noblenet-web");
  ///<summary>ngdat</summary>
  public static MimeEntry Ngdat { get; } = new(["ngdat"], "application/vnd.nokia.n-gage.data");
  ///<summary>n-gage</summary>
  public static MimeEntry Ngage { get; } = new(["n-gage"], "application/vnd.nokia.n-gage.symbian.install");
  ///<summary>rpst</summary>
  public static MimeEntry Rpst { get; } = new(["rpst"], "application/vnd.nokia.radio-preset");
  ///<summary>rpss</summary>
  public static MimeEntry Rpss { get; } = new(["rpss"], "application/vnd.nokia.radio-presets");
  ///<summary>edm</summary>
  public static MimeEntry Edm { get; } = new(["edm"], "application/vnd.novadigm.edm");
  ///<summary>edx</summary>
  public static MimeEntry Edx { get; } = new(["edx"], "application/vnd.novadigm.edx");
  ///<summary>ext</summary>
  public static MimeEntry Ext { get; } = new(["ext"], "application/vnd.novadigm.ext");
  ///<summary>odc</summary>
  public static MimeEntry Odc { get; } = new(["odc"], "application/vnd.oasis.opendocument.chart");
  ///<summary>otc</summary>
  public static MimeEntry Otc { get; } = new(["otc"], "application/vnd.oasis.opendocument.chart-template");
  ///<summary>odb</summary>
  public static MimeEntry Odb { get; } = new(["odb"], "application/vnd.oasis.opendocument.database");
  ///<summary>odf</summary>
  public static MimeEntry Odf { get; } = new(["odf"], "application/vnd.oasis.opendocument.formula");
  ///<summary>odft</summary>
  public static MimeEntry Odft { get; } = new(["odft"], "application/vnd.oasis.opendocument.formula-template");
  ///<summary>odg</summary>
  public static MimeEntry Odg { get; } = new(["odg"], "application/vnd.oasis.opendocument.graphics");
  ///<summary>otg</summary>
  public static MimeEntry Otg { get; } = new(["otg"], "application/vnd.oasis.opendocument.graphics-template");
  ///<summary>odi</summary>
  public static MimeEntry Odi { get; } = new(["odi"], "application/vnd.oasis.opendocument.image");
  ///<summary>oti</summary>
  public static MimeEntry Oti { get; } = new(["oti"], "application/vnd.oasis.opendocument.image-template");
  ///<summary>odp</summary>
  public static MimeEntry Odp { get; } = new(["odp"], "application/vnd.oasis.opendocument.presentation");
  ///<summary>otp</summary>
  public static MimeEntry Otp { get; } = new(["otp"], "application/vnd.oasis.opendocument.presentation-template");
  ///<summary>ods</summary>
  public static MimeEntry Ods { get; } = new(["ods"], "application/vnd.oasis.opendocument.spreadsheet");
  ///<summary>ots</summary>
  public static MimeEntry Ots { get; } = new(["ots"], "application/vnd.oasis.opendocument.spreadsheet-template");
  ///<summary>odt</summary>
  public static MimeEntry Odt { get; } = new(["odt"], "application/vnd.oasis.opendocument.text");
  ///<summary>odm</summary>
  public static MimeEntry Odm { get; } = new(["odm"], "application/vnd.oasis.opendocument.text-master");
  ///<summary>ott</summary>
  public static MimeEntry Ott { get; } = new(["ott"], "application/vnd.oasis.opendocument.text-template");
  ///<summary>oth</summary>
  public static MimeEntry Oth { get; } = new(["oth"], "application/vnd.oasis.opendocument.text-web");
  ///<summary>xo</summary>
  public static MimeEntry Xo { get; } = new(["xo"], "application/vnd.olpc-sugar");
  ///<summary>dd2</summary>
  public static MimeEntry Dd2 { get; } = new(["dd2"], "application/vnd.oma.dd2+xml");
  ///<summary>oxt</summary>
  public static MimeEntry Oxt { get; } = new(["oxt"], "application/vnd.openofficeorg.extension");
  ///<summary>pptx</summary>
  public static MimeEntry Pptx { get; } = new(["pptx"], "application/vnd.openxmlformats-officedocument.presentationml.presentation");
  ///<summary>sldx</summary>
  public static MimeEntry Sldx { get; } = new(["sldx"], "application/vnd.openxmlformats-officedocument.presentationml.slide");
  ///<summary>ppsx</summary>
  public static MimeEntry Ppsx { get; } = new(["ppsx"], "application/vnd.openxmlformats-officedocument.presentationml.slideshow");
  ///<summary>potx</summary>
  public static MimeEntry Potx { get; } = new(["potx"], "application/vnd.openxmlformats-officedocument.presentationml.template");
  ///<summary>xlsx</summary>
  public static MimeEntry Xlsx { get; } = new(["xlsx"], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
  ///<summary>xltx</summary>
  public static MimeEntry Xltx { get; } = new(["xltx"], "application/vnd.openxmlformats-officedocument.spreadsheetml.template");
  ///<summary>docx</summary>
  public static MimeEntry Docx { get; } = new(["docx"], "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
  ///<summary>dotx</summary>
  public static MimeEntry Dotx { get; } = new(["dotx"], "application/vnd.openxmlformats-officedocument.wordprocessingml.template");
  ///<summary>mgp</summary>
  public static MimeEntry Mgp { get; } = new(["mgp"], "application/vnd.osgeo.mapguide.package");
  ///<summary>dp</summary>
  public static MimeEntry Dp { get; } = new(["dp"], "application/vnd.osgi.dp");
  ///<summary>esa</summary>
  public static MimeEntry Esa { get; } = new(["esa"], "application/vnd.osgi.subsystem");
  ///<summary>pdb</summary>
  public static MimeEntry Pdb { get; } = new(["pdb", "pqa", "oprc"], "application/vnd.palm");
  ///<summary>pqa</summary>
  public static MimeEntry Pqa { get; } = new(["pdb", "pqa", "oprc"], "application/vnd.palm");
  ///<summary>oprc</summary>
  public static MimeEntry Oprc { get; } = new(["pdb", "pqa", "oprc"], "application/vnd.palm");
  ///<summary>paw</summary>
  public static MimeEntry Paw { get; } = new(["paw"], "application/vnd.pawaafile");
  ///<summary>str</summary>
  public static MimeEntry Str { get; } = new(["str"], "application/vnd.pg.format");
  ///<summary>ei6</summary>
  public static MimeEntry Ei6 { get; } = new(["ei6"], "application/vnd.pg.osasli");
  ///<summary>efif</summary>
  public static MimeEntry Efif { get; } = new(["efif"], "application/vnd.picsel");
  ///<summary>wg</summary>
  public static MimeEntry Wg { get; } = new(["wg"], "application/vnd.pmi.widget");
  ///<summary>plf</summary>
  public static MimeEntry Plf { get; } = new(["plf"], "application/vnd.pocketlearn");
  ///<summary>pbd</summary>
  public static MimeEntry Pbd { get; } = new(["pbd"], "application/vnd.powerbuilder6");
  ///<summary>box</summary>
  public static MimeEntry Box { get; } = new(["box"], "application/vnd.previewsystems.box");
  ///<summary>mgz</summary>
  public static MimeEntry Mgz { get; } = new(["mgz"], "application/vnd.proteus.magazine");
  ///<summary>qps</summary>
  public static MimeEntry Qps { get; } = new(["qps"], "application/vnd.publishare-delta-tree");
  ///<summary>ptid</summary>
  public static MimeEntry Ptid { get; } = new(["ptid"], "application/vnd.pvi.ptid1");
  ///<summary>qxd</summary>
  public static MimeEntry Qxd { get; } = new(["qxd", "qxt", "qwd", "qwt", "qxl", "qxb"], "application/vnd.quark.quarkxpress");
  ///<summary>qxt</summary>
  public static MimeEntry Qxt { get; } = new(["qxd", "qxt", "qwd", "qwt", "qxl", "qxb"], "application/vnd.quark.quarkxpress");
  ///<summary>qwd</summary>
  public static MimeEntry Qwd { get; } = new(["qxd", "qxt", "qwd", "qwt", "qxl", "qxb"], "application/vnd.quark.quarkxpress");
  ///<summary>qwt</summary>
  public static MimeEntry Qwt { get; } = new(["qxd", "qxt", "qwd", "qwt", "qxl", "qxb"], "application/vnd.quark.quarkxpress");
  ///<summary>qxl</summary>
  public static MimeEntry Qxl { get; } = new(["qxd", "qxt", "qwd", "qwt", "qxl", "qxb"], "application/vnd.quark.quarkxpress");
  ///<summary>qxb</summary>
  public static MimeEntry Qxb { get; } = new(["qxd", "qxt", "qwd", "qwt", "qxl", "qxb"], "application/vnd.quark.quarkxpress");
  ///<summary>bed</summary>
  public static MimeEntry Bed { get; } = new(["bed"], "application/vnd.realvnc.bed");
  ///<summary>mxl</summary>
  public static MimeEntry Mxl { get; } = new(["mxl"], "application/vnd.recordare.musicxml");
  ///<summary>musicxml</summary>
  public static MimeEntry Musicxml { get; } = new(["musicxml"], "application/vnd.recordare.musicxml+xml");
  ///<summary>cryptonote</summary>
  public static MimeEntry Cryptonote { get; } = new(["cryptonote"], "application/vnd.rig.cryptonote");
  ///<summary>cod</summary>
  public static MimeEntry Cod { get; } = new(["cod"], "application/vnd.rim.cod");
  ///<summary>rm</summary>
  public static MimeEntry Rm { get; } = new(["rm"], "application/vnd.rn-realmedia");
  ///<summary>rmvb</summary>
  public static MimeEntry Rmvb { get; } = new(["rmvb"], "application/vnd.rn-realmedia-vbr");
  ///<summary>link66</summary>
  public static MimeEntry Link66 { get; } = new(["link66"], "application/vnd.route66.link66+xml");
  ///<summary>st</summary>
  public static MimeEntry St { get; } = new(["st"], "application/vnd.sailingtracker.track");
  ///<summary>see</summary>
  public static MimeEntry See { get; } = new(["see"], "application/vnd.seemail");
  ///<summary>sema</summary>
  public static MimeEntry Sema { get; } = new(["sema"], "application/vnd.sema");
  ///<summary>semd</summary>
  public static MimeEntry Semd { get; } = new(["semd"], "application/vnd.semd");
  ///<summary>semf</summary>
  public static MimeEntry Semf { get; } = new(["semf"], "application/vnd.semf");
  ///<summary>ifm</summary>
  public static MimeEntry Ifm { get; } = new(["ifm"], "application/vnd.shana.informed.formdata");
  ///<summary>itp</summary>
  public static MimeEntry Itp { get; } = new(["itp"], "application/vnd.shana.informed.formtemplate");
  ///<summary>iif</summary>
  public static MimeEntry Iif { get; } = new(["iif"], "application/vnd.shana.informed.interchange");
  ///<summary>ipk</summary>
  public static MimeEntry Ipk { get; } = new(["ipk"], "application/vnd.shana.informed.package");
  ///<summary>twd</summary>
  public static MimeEntry Twd { get; } = new(["twd", "twds"], "application/vnd.simtech-mindmapper");
  ///<summary>twds</summary>
  public static MimeEntry Twds { get; } = new(["twd", "twds"], "application/vnd.simtech-mindmapper");
  ///<summary>mmf</summary>
  public static MimeEntry Mmf { get; } = new(["mmf"], "application/vnd.smaf");
  ///<summary>teacher</summary>
  public static MimeEntry Teacher { get; } = new(["teacher"], "application/vnd.smart.teacher");
  ///<summary>sdkm</summary>
  public static MimeEntry Sdkm { get; } = new(["sdkm", "sdkd"], "application/vnd.solent.sdkm+xml");
  ///<summary>sdkd</summary>
  public static MimeEntry Sdkd { get; } = new(["sdkm", "sdkd"], "application/vnd.solent.sdkm+xml");
  ///<summary>dxp</summary>
  public static MimeEntry Dxp { get; } = new(["dxp"], "application/vnd.spotfire.dxp");
  ///<summary>sfs</summary>
  public static MimeEntry Sfs { get; } = new(["sfs"], "application/vnd.spotfire.sfs");
  ///<summary>sdc</summary>
  public static MimeEntry Sdc { get; } = new(["sdc"], "application/vnd.stardivision.calc");
  ///<summary>sda</summary>
  public static MimeEntry Sda { get; } = new(["sda"], "application/vnd.stardivision.draw");
  ///<summary>sdd</summary>
  public static MimeEntry Sdd { get; } = new(["sdd"], "application/vnd.stardivision.impress");
  ///<summary>smf</summary>
  public static MimeEntry Smf { get; } = new(["smf"], "application/vnd.stardivision.math");
  ///<summary>sdw</summary>
  public static MimeEntry Sdw { get; } = new(["sdw", "vor"], "application/vnd.stardivision.writer");
  ///<summary>vor</summary>
  public static MimeEntry Vor { get; } = new(["sdw", "vor"], "application/vnd.stardivision.writer");
  ///<summary>sgl</summary>
  public static MimeEntry Sgl { get; } = new(["sgl"], "application/vnd.stardivision.writer-global");
  ///<summary>smzip</summary>
  public static MimeEntry Smzip { get; } = new(["smzip"], "application/vnd.stepmania.package");
  ///<summary>sm</summary>
  public static MimeEntry Sm { get; } = new(["sm"], "application/vnd.stepmania.stepchart");
  ///<summary>sxc</summary>
  public static MimeEntry Sxc { get; } = new(["sxc"], "application/vnd.sun.xml.calc");
  ///<summary>stc</summary>
  public static MimeEntry Stc { get; } = new(["stc"], "application/vnd.sun.xml.calc.template");
  ///<summary>sxd</summary>
  public static MimeEntry Sxd { get; } = new(["sxd"], "application/vnd.sun.xml.draw");
  ///<summary>std</summary>
  public static MimeEntry Std { get; } = new(["std"], "application/vnd.sun.xml.draw.template");
  ///<summary>sxi</summary>
  public static MimeEntry Sxi { get; } = new(["sxi"], "application/vnd.sun.xml.impress");
  ///<summary>sti</summary>
  public static MimeEntry Sti { get; } = new(["sti"], "application/vnd.sun.xml.impress.template");
  ///<summary>sxm</summary>
  public static MimeEntry Sxm { get; } = new(["sxm"], "application/vnd.sun.xml.math");
  ///<summary>sxw</summary>
  public static MimeEntry Sxw { get; } = new(["sxw"], "application/vnd.sun.xml.writer");
  ///<summary>sxg</summary>
  public static MimeEntry Sxg { get; } = new(["sxg"], "application/vnd.sun.xml.writer.global");
  ///<summary>stw</summary>
  public static MimeEntry Stw { get; } = new(["stw"], "application/vnd.sun.xml.writer.template");
  ///<summary>sus</summary>
  public static MimeEntry Sus { get; } = new(["sus", "susp"], "application/vnd.sus-calendar");
  ///<summary>susp</summary>
  public static MimeEntry Susp { get; } = new(["sus", "susp"], "application/vnd.sus-calendar");
  ///<summary>svd</summary>
  public static MimeEntry Svd { get; } = new(["svd"], "application/vnd.svd");
  ///<summary>sis</summary>
  public static MimeEntry Sis { get; } = new(["sis", "sisx"], "application/vnd.symbian.install");
  ///<summary>sisx</summary>
  public static MimeEntry Sisx { get; } = new(["sis", "sisx"], "application/vnd.symbian.install");
  ///<summary>xsm</summary>
  public static MimeEntry Xsm { get; } = new(["xsm"], "application/vnd.syncml+xml");
  ///<summary>bdm</summary>
  public static MimeEntry Bdm { get; } = new(["bdm"], "application/vnd.syncml.dm+wbxml");
  ///<summary>xdm</summary>
  public static MimeEntry Xdm { get; } = new(["xdm"], "application/vnd.syncml.dm+xml");
  ///<summary>tao</summary>
  public static MimeEntry Tao { get; } = new(["tao"], "application/vnd.tao.intent-module-archive");
  ///<summary>pcap</summary>
  public static MimeEntry Pcap { get; } = new(["pcap", "cap", "dmp"], "application/vnd.tcpdump.pcap");
  ///<summary>cap</summary>
  public static MimeEntry Cap { get; } = new(["pcap", "cap", "dmp"], "application/vnd.tcpdump.pcap");
  ///<summary>dmp</summary>
  public static MimeEntry Dmp { get; } = new(["pcap", "cap", "dmp"], "application/vnd.tcpdump.pcap");
  ///<summary>tmo</summary>
  public static MimeEntry Tmo { get; } = new(["tmo"], "application/vnd.tmobile-livetv");
  ///<summary>tpt</summary>
  public static MimeEntry Tpt { get; } = new(["tpt"], "application/vnd.trid.tpt");
  ///<summary>mxs</summary>
  public static MimeEntry Mxs { get; } = new(["mxs"], "application/vnd.triscape.mxs");
  ///<summary>tra</summary>
  public static MimeEntry Tra { get; } = new(["tra"], "application/vnd.trueapp");
  ///<summary>ufd</summary>
  public static MimeEntry Ufd { get; } = new(["ufd", "ufdl"], "application/vnd.ufdl");
  ///<summary>ufdl</summary>
  public static MimeEntry Ufdl { get; } = new(["ufd", "ufdl"], "application/vnd.ufdl");
  ///<summary>utz</summary>
  public static MimeEntry Utz { get; } = new(["utz"], "application/vnd.uiq.theme");
  ///<summary>umj</summary>
  public static MimeEntry Umj { get; } = new(["umj"], "application/vnd.umajin");
  ///<summary>unityweb</summary>
  public static MimeEntry Unityweb { get; } = new(["unityweb"], "application/vnd.unity");
  ///<summary>uoml</summary>
  public static MimeEntry Uoml { get; } = new(["uoml", "uo"], "application/vnd.uoml+xml");
  ///<summary>vcx</summary>
  public static MimeEntry Vcx { get; } = new(["vcx"], "application/vnd.vcx");
  ///<summary>vsd</summary>
  public static MimeEntry Vsd { get; } = new(["vsd", "vst", "vss", "vsw"], "application/vnd.visio");
  ///<summary>vst</summary>
  public static MimeEntry Vst { get; } = new(["vsd", "vst", "vss", "vsw"], "application/vnd.visio");
  ///<summary>vss</summary>
  public static MimeEntry Vss { get; } = new(["vsd", "vst", "vss", "vsw"], "application/vnd.visio");
  ///<summary>vsw</summary>
  public static MimeEntry Vsw { get; } = new(["vsd", "vst", "vss", "vsw"], "application/vnd.visio");
  ///<summary>vis</summary>
  public static MimeEntry Vis { get; } = new(["vis"], "application/vnd.visionary");
  ///<summary>vsf</summary>
  public static MimeEntry Vsf { get; } = new(["vsf"], "application/vnd.vsf");
  ///<summary>wbxml</summary>
  public static MimeEntry Wbxml { get; } = new(["wbxml"], "application/vnd.wap.wbxml");
  ///<summary>wmlc</summary>
  public static MimeEntry Wmlc { get; } = new(["wmlc"], "application/vnd.wap.wmlc");
  ///<summary>wmlsc</summary>
  public static MimeEntry Wmlsc { get; } = new(["wmlsc"], "application/vnd.wap.wmlscriptc");
  ///<summary>wtb</summary>
  public static MimeEntry Wtb { get; } = new(["wtb"], "application/vnd.webturbo");
  ///<summary>nbp</summary>
  public static MimeEntry Nbp { get; } = new(["nbp"], "application/vnd.wolfram.player");
  ///<summary>wpd</summary>
  public static MimeEntry Wpd { get; } = new(["wpd"], "application/vnd.wordperfect");
  ///<summary>wqd</summary>
  public static MimeEntry Wqd { get; } = new(["wqd"], "application/vnd.wqd");
  ///<summary>stf</summary>
  public static MimeEntry Stf { get; } = new(["stf"], "application/vnd.wt.stf");
  ///<summary>xar</summary>
  public static MimeEntry Xar { get; } = new(["xar"], "application/vnd.xara");
  ///<summary>xfdl</summary>
  public static MimeEntry Xfdl { get; } = new(["xfdl"], "application/vnd.xfdl");
  ///<summary>hvd</summary>
  public static MimeEntry Hvd { get; } = new(["hvd"], "application/vnd.yamaha.hv-dic");
  ///<summary>hvs</summary>
  public static MimeEntry Hvs { get; } = new(["hvs"], "application/vnd.yamaha.hv-script");
  ///<summary>hvp</summary>
  public static MimeEntry Hvp { get; } = new(["hvp"], "application/vnd.yamaha.hv-voice");
  ///<summary>osf</summary>
  public static MimeEntry Osf { get; } = new(["osf"], "application/vnd.yamaha.openscoreformat");
  ///<summary>osfpvg</summary>
  public static MimeEntry Osfpvg { get; } = new(["osfpvg"], "application/vnd.yamaha.openscoreformat.osfpvg+xml");
  ///<summary>saf</summary>
  public static MimeEntry Saf { get; } = new(["saf"], "application/vnd.yamaha.smaf-audio");
  ///<summary>spf</summary>
  public static MimeEntry Spf { get; } = new(["spf"], "application/vnd.yamaha.smaf-phrase");
  ///<summary>cmp</summary>
  public static MimeEntry Cmp { get; } = new(["cmp"], "application/vnd.yellowriver-custom-menu");
  ///<summary>zir</summary>
  public static MimeEntry Zir { get; } = new(["zir", "zirz"], "application/vnd.zul");
  ///<summary>zirz</summary>
  public static MimeEntry Zirz { get; } = new(["zir", "zirz"], "application/vnd.zul");
  ///<summary>zaz</summary>
  public static MimeEntry Zaz { get; } = new(["zaz"], "application/vnd.zzazz.deck+xml");
  ///<summary>vxml</summary>
  public static MimeEntry Vxml { get; } = new(["vxml"], "application/voicexml+xml");
  ///<summary>wgt</summary>
  public static MimeEntry Wgt { get; } = new(["wgt"], "application/widget");
  ///<summary>hlp</summary>
  public static MimeEntry Hlp { get; } = new(["hlp"], "application/winhlp");
  ///<summary>wsdl</summary>
  public static MimeEntry Wsdl { get; } = new(["wsdl"], "application/wsdl+xml");
  ///<summary>wspolicy</summary>
  public static MimeEntry Wspolicy { get; } = new(["wspolicy"], "application/wspolicy+xml");
  ///<summary>7z</summary>
  public static MimeEntry _7z { get; } = new(["7z"], "application/x-7z-compressed");
  ///<summary>abw</summary>
  public static MimeEntry Abw { get; } = new(["abw"], "application/x-abiword");
  ///<summary>ace</summary>
  public static MimeEntry Ace { get; } = new(["ace"], "application/x-ace-compressed");
  ///<summary>dmg</summary>
  public static MimeEntry Dmg { get; } = new(["dmg"], "application/x-apple-diskimage");
  ///<summary>aab</summary>
  public static MimeEntry Aab { get; } = new(["aab", "x32", "u32", "vox"], "application/x-authorware-bin");
  ///<summary>x32</summary>
  public static MimeEntry X32 { get; } = new(["aab", "x32", "u32", "vox"], "application/x-authorware-bin");
  ///<summary>u32</summary>
  public static MimeEntry U32 { get; } = new(["aab", "x32", "u32", "vox"], "application/x-authorware-bin");
  ///<summary>vox</summary>
  public static MimeEntry Vox { get; } = new(["aab", "x32", "u32", "vox"], "application/x-authorware-bin");
  ///<summary>aam</summary>
  public static MimeEntry Aam { get; } = new(["aam"], "application/x-authorware-map");
  ///<summary>aas</summary>
  public static MimeEntry Aas { get; } = new(["aas"], "application/x-authorware-seg");
  ///<summary>bcpio</summary>
  public static MimeEntry Bcpio { get; } = new(["bcpio"], "application/x-bcpio");
  ///<summary>torrent</summary>
  public static MimeEntry Torrent { get; } = new(["torrent"], "application/x-bittorrent");
  ///<summary>blb</summary>
  public static MimeEntry Blb { get; } = new(["blb", "blorb"], "application/x-blorb");
  ///<summary>blorb</summary>
  public static MimeEntry Blorb { get; } = new(["blb", "blorb"], "application/x-blorb");
  ///<summary>bz</summary>
  public static MimeEntry Bz { get; } = new(["bz"], "application/x-bzip");
  ///<summary>bz2</summary>
  public static MimeEntry Bz2 { get; } = new(["bz2", "boz"], "application/x-bzip2");
  ///<summary>boz</summary>
  public static MimeEntry Boz { get; } = new(["bz2", "boz"], "application/x-bzip2");
  ///<summary>cbr</summary>
  public static MimeEntry Cbr { get; } = new(["cbr", "cba", "cbt", "cbz", "cb7"], "application/x-cbr");
  ///<summary>cba</summary>
  public static MimeEntry Cba { get; } = new(["cbr", "cba", "cbt", "cbz", "cb7"], "application/x-cbr");
  ///<summary>cbt</summary>
  public static MimeEntry Cbt { get; } = new(["cbr", "cba", "cbt", "cbz", "cb7"], "application/x-cbr");
  ///<summary>cbz</summary>
  public static MimeEntry Cbz { get; } = new(["cbr", "cba", "cbt", "cbz", "cb7"], "application/x-cbr");
  ///<summary>cb7</summary>
  public static MimeEntry Cb7 { get; } = new(["cbr", "cba", "cbt", "cbz", "cb7"], "application/x-cbr");
  ///<summary>vcd</summary>
  public static MimeEntry Vcd { get; } = new(["vcd"], "application/x-cdlink");
  ///<summary>cfs</summary>
  public static MimeEntry Cfs { get; } = new(["cfs"], "application/x-cfs-compressed");
  ///<summary>chat</summary>
  public static MimeEntry Chat { get; } = new(["chat"], "application/x-chat");
  ///<summary>pgn</summary>
  public static MimeEntry Pgn { get; } = new(["pgn"], "application/x-chess-pgn");
  ///<summary>nsc</summary>
  public static MimeEntry Nsc { get; } = new(["nsc"], "application/x-conference");
  ///<summary>cpio</summary>
  public static MimeEntry Cpio { get; } = new(["cpio"], "application/x-cpio");
  ///<summary>csh</summary>
  public static MimeEntry Csh { get; } = new(["csh"], "application/x-csh");
  ///<summary>deb</summary>
  public static MimeEntry Deb { get; } = new(["udeb"], "application/x-debian-package");
  ///<summary>udeb</summary>
  public static MimeEntry Udeb { get; } = new(["udeb"], "application/x-debian-package");
  ///<summary>dgc</summary>
  public static MimeEntry Dgc { get; } = new(["dgc"], "application/x-dgc-compressed");
  ///<summary>dir</summary>
  public static MimeEntry Dir { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>dcr</summary>
  public static MimeEntry Dcr { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>dxr</summary>
  public static MimeEntry Dxr { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>cst</summary>
  public static MimeEntry Cst { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>cct</summary>
  public static MimeEntry Cct { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>cxt</summary>
  public static MimeEntry Cxt { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>w3d</summary>
  public static MimeEntry W3d { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>fgd</summary>
  public static MimeEntry Fgd { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>swa</summary>
  public static MimeEntry Swa { get; } = new(["dir", "dcr", "dxr", "cst", "cct", "cxt", "w3d", "fgd", "swa"], "application/x-director");
  ///<summary>wad</summary>
  public static MimeEntry Wad { get; } = new(["wad"], "application/x-doom");
  ///<summary>ncx</summary>
  public static MimeEntry Ncx { get; } = new(["ncx"], "application/x-dtbncx+xml");
  ///<summary>dtb</summary>
  public static MimeEntry Dtb { get; } = new(["dtb"], "application/x-dtbook+xml");
  ///<summary>res</summary>
  public static MimeEntry Res { get; } = new(["res"], "application/x-dtbresource+xml");
  ///<summary>dvi</summary>
  public static MimeEntry Dvi { get; } = new(["dvi"], "application/x-dvi");
  ///<summary>evy</summary>
  public static MimeEntry Evy { get; } = new(["evy"], "application/x-envoy");
  ///<summary>eva</summary>
  public static MimeEntry Eva { get; } = new(["eva"], "application/x-eva");
  ///<summary>bdf</summary>
  public static MimeEntry Bdf { get; } = new(["bdf"], "application/x-font-bdf");
  ///<summary>gsf</summary>
  public static MimeEntry Gsf { get; } = new(["gsf"], "application/x-font-ghostscript");
  ///<summary>psf</summary>
  public static MimeEntry Psf { get; } = new(["psf"], "application/x-font-linux-psf");
  ///<summary>pcf</summary>
  public static MimeEntry Pcf { get; } = new(["pcf"], "application/x-font-pcf");
  ///<summary>snf</summary>
  public static MimeEntry Snf { get; } = new(["snf"], "application/x-font-snf");
  ///<summary>pfa</summary>
  public static MimeEntry Pfa { get; } = new(["pfa", "pfb", "pfm", "afm"], "application/x-font-type1");
  ///<summary>pfb</summary>
  public static MimeEntry Pfb { get; } = new(["pfa", "pfb", "pfm", "afm"], "application/x-font-type1");
  ///<summary>pfm</summary>
  public static MimeEntry Pfm { get; } = new(["pfa", "pfb", "pfm", "afm"], "application/x-font-type1");
  ///<summary>afm</summary>
  public static MimeEntry Afm { get; } = new(["pfa", "pfb", "pfm", "afm"], "application/x-font-type1");
  ///<summary>arc</summary>
  public static MimeEntry Arc { get; } = new(["arc"], "application/x-freearc");
  ///<summary>spl</summary>
  public static MimeEntry Spl { get; } = new(["spl"], "application/x-futuresplash");
  ///<summary>gca</summary>
  public static MimeEntry Gca { get; } = new(["gca"], "application/x-gca-compressed");
  ///<summary>ulx</summary>
  public static MimeEntry Ulx { get; } = new(["ulx"], "application/x-glulx");
  ///<summary>gnumeric</summary>
  public static MimeEntry Gnumeric { get; } = new(["gnumeric"], "application/x-gnumeric");
  ///<summary>gramps</summary>
  public static MimeEntry Gramps { get; } = new(["gramps"], "application/x-gramps-xml");
  ///<summary>gtar</summary>
  public static MimeEntry Gtar { get; } = new(["gtar"], "application/x-gtar");
  ///<summary>hdf</summary>
  public static MimeEntry Hdf { get; } = new(["hdf"], "application/x-hdf");
  ///<summary>install</summary>
  public static MimeEntry Install { get; } = new(["install"], "application/x-install-instructions");
  ///<summary>iso</summary>
  public static MimeEntry Iso { get; } = new(["iso"], "application/x-iso9660-image");
  ///<summary>jnlp</summary>
  public static MimeEntry Jnlp { get; } = new(["jnlp"], "application/x-java-jnlp-file");
  ///<summary>latex</summary>
  public static MimeEntry Latex { get; } = new(["latex"], "application/x-latex");
  ///<summary>lzh</summary>
  public static MimeEntry Lzh { get; } = new(["lzh", "lha"], "application/x-lzh-compressed");
  ///<summary>lha</summary>
  public static MimeEntry Lha { get; } = new(["lzh", "lha"], "application/x-lzh-compressed");
  ///<summary>mie</summary>
  public static MimeEntry Mie { get; } = new(["mie"], "application/x-mie");
  ///<summary>prc</summary>
  public static MimeEntry Prc { get; } = new(["prc", "mobi"], "application/x-mobipocket-ebook");
  ///<summary>mobi</summary>
  public static MimeEntry Mobi { get; } = new(["prc", "mobi"], "application/x-mobipocket-ebook");
  ///<summary>application</summary>
  public static MimeEntry Application { get; } = new(["application"], "application/x-ms-application");
  ///<summary>lnk</summary>
  public static MimeEntry Lnk { get; } = new(["lnk"], "application/x-ms-shortcut");
  ///<summary>wmd</summary>
  public static MimeEntry Wmd { get; } = new(["wmd"], "application/x-ms-wmd");
  ///<summary>wmz</summary>
  public static MimeEntry Wmz { get; } = new(["wmz"], "application/x-ms-wmz");
  ///<summary>xbap</summary>
  public static MimeEntry Xbap { get; } = new(["xbap"], "application/x-ms-xbap");
  ///<summary>mdb</summary>
  public static MimeEntry Mdb { get; } = new(["mdb"], "application/x-msaccess");
  ///<summary>obd</summary>
  public static MimeEntry Obd { get; } = new(["obd"], "application/x-msbinder");
  ///<summary>crd</summary>
  public static MimeEntry Crd { get; } = new(["crd"], "application/x-mscardfile");
  ///<summary>clp</summary>
  public static MimeEntry Clp { get; } = new(["clp"], "application/x-msclip");
  ///<summary>exe</summary>
  public static MimeEntry Exe { get; } = new(["com", "bat"], "application/x-msdownload");
  ///<summary>dll</summary>
  public static MimeEntry Dll { get; } = new(["com", "bat"], "application/x-msdownload");
  ///<summary>com</summary>
  public static MimeEntry Com { get; } = new(["com", "bat"], "application/x-msdownload");
  ///<summary>bat</summary>
  public static MimeEntry Bat { get; } = new(["com", "bat"], "application/x-msdownload");
  ///<summary>msi</summary>
  public static MimeEntry Msi { get; } = new(["com", "bat"], "application/x-msdownload");
  ///<summary>mvb</summary>
  public static MimeEntry Mvb { get; } = new(["mvb", "m13", "m14"], "application/x-msmediaview");
  ///<summary>m13</summary>
  public static MimeEntry M13 { get; } = new(["mvb", "m13", "m14"], "application/x-msmediaview");
  ///<summary>m14</summary>
  public static MimeEntry M14 { get; } = new(["mvb", "m13", "m14"], "application/x-msmediaview");
  ///<summary>wmf</summary>
  public static MimeEntry Wmf { get; } = new(["wmf", "emf", "emz"], "application/x-msmetafile");
  ///<summary>emf</summary>
  public static MimeEntry Emf { get; } = new(["wmf", "emf", "emz"], "application/x-msmetafile");
  ///<summary>emz</summary>
  public static MimeEntry Emz { get; } = new(["wmf", "emf", "emz"], "application/x-msmetafile");
  ///<summary>mny</summary>
  public static MimeEntry Mny { get; } = new(["mny"], "application/x-msmoney");
  ///<summary>pub</summary>
  public static MimeEntry Pub { get; } = new(["pub"], "application/x-mspublisher");
  ///<summary>scd</summary>
  public static MimeEntry Scd { get; } = new(["scd"], "application/x-msschedule");
  ///<summary>trm</summary>
  public static MimeEntry Trm { get; } = new(["trm"], "application/x-msterminal");
  ///<summary>wri</summary>
  public static MimeEntry Wri { get; } = new(["wri"], "application/x-mswrite");
  ///<summary>nc</summary>
  public static MimeEntry Nc { get; } = new(["nc", "cdf"], "application/x-netcdf");
  ///<summary>cdf</summary>
  public static MimeEntry Cdf { get; } = new(["nc", "cdf"], "application/x-netcdf");
  ///<summary>nzb</summary>
  public static MimeEntry Nzb { get; } = new(["nzb"], "application/x-nzb");
  ///<summary>p12</summary>
  public static MimeEntry P12 { get; } = new(["p12", "pfx"], "application/x-pkcs12");
  ///<summary>pfx</summary>
  public static MimeEntry Pfx { get; } = new(["p12", "pfx"], "application/x-pkcs12");
  ///<summary>p7b</summary>
  public static MimeEntry P7b { get; } = new(["p7b", "spc"], "application/x-pkcs7-certificates");
  ///<summary>spc</summary>
  public static MimeEntry Spc { get; } = new(["p7b", "spc"], "application/x-pkcs7-certificates");
  ///<summary>p7r</summary>
  public static MimeEntry P7r { get; } = new(["p7r"], "application/x-pkcs7-certreqresp");
  ///<summary>rar</summary>
  public static MimeEntry Rar { get; } = new(["rar"], "application/x-rar-compressed");
  ///<summary>ris</summary>
  public static MimeEntry Ris { get; } = new(["ris"], "application/x-research-info-systems");
  ///<summary>sh</summary>
  public static MimeEntry Sh { get; } = new(["sh"], "application/x-sh");
  ///<summary>shar</summary>
  public static MimeEntry Shar { get; } = new(["shar"], "application/x-shar");
  ///<summary>swf</summary>
  public static MimeEntry Swf { get; } = new(["swf"], "application/x-shockwave-flash");
  ///<summary>xap</summary>
  public static MimeEntry Xap { get; } = new(["xap"], "application/x-silverlight-app");
  ///<summary>sql</summary>
  public static MimeEntry Sql { get; } = new(["sql"], "application/x-sql");
  ///<summary>sit</summary>
  public static MimeEntry Sit { get; } = new(["sit"], "application/x-stuffit");
  ///<summary>sitx</summary>
  public static MimeEntry Sitx { get; } = new(["sitx"], "application/x-stuffitx");
  ///<summary>srt</summary>
  public static MimeEntry Srt { get; } = new(["srt"], "application/x-subrip");
  ///<summary>sv4cpio</summary>
  public static MimeEntry Sv4cpio { get; } = new(["sv4cpio"], "application/x-sv4cpio");
  ///<summary>sv4crc</summary>
  public static MimeEntry Sv4crc { get; } = new(["sv4crc"], "application/x-sv4crc");
  ///<summary>t3</summary>
  public static MimeEntry T3 { get; } = new(["t3"], "application/x-t3vm-image");
  ///<summary>gam</summary>
  public static MimeEntry Gam { get; } = new(["gam"], "application/x-tads");
  ///<summary>tar</summary>
  public static MimeEntry Tar { get; } = new(["tar"], "application/x-tar");
  ///<summary>tcl</summary>
  public static MimeEntry Tcl { get; } = new(["tcl", "tk"], "application/x-tcl");
  ///<summary>tex</summary>
  public static MimeEntry Tex { get; } = new(["tex"], "application/x-tex");
  ///<summary>tfm</summary>
  public static MimeEntry Tfm { get; } = new(["tfm"], "application/x-tex-tfm");
  ///<summary>texinfo</summary>
  public static MimeEntry Texinfo { get; } = new(["texinfo", "texi"], "application/x-texinfo");
  ///<summary>texi</summary>
  public static MimeEntry Texi { get; } = new(["texinfo", "texi"], "application/x-texinfo");
  ///<summary>obj</summary>
  public static MimeEntry Obj { get; } = new(["obj"], "application/x-tgif");
  ///<summary>ustar</summary>
  public static MimeEntry Ustar { get; } = new(["ustar"], "application/x-ustar");
  ///<summary>src</summary>
  public static MimeEntry Src { get; } = new(["src"], "application/x-wais-source");
  ///<summary>der</summary>
  public static MimeEntry Der { get; } = new(["der", "crt", "pem"], "application/x-x509-ca-cert");
  ///<summary>crt</summary>
  public static MimeEntry Crt { get; } = new(["der", "crt", "pem"], "application/x-x509-ca-cert");
  ///<summary>fig</summary>
  public static MimeEntry Fig { get; } = new(["fig"], "application/x-xfig");
  ///<summary>xlf</summary>
  public static MimeEntry Xlf { get; } = new(["xlf"], "application/x-xliff+xml");
  ///<summary>xpi</summary>
  public static MimeEntry Xpi { get; } = new(["xpi"], "application/x-xpinstall");
  ///<summary>xz</summary>
  public static MimeEntry Xz { get; } = new(["xz"], "application/x-xz");
  ///<summary>z1</summary>
  public static MimeEntry Z1 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z2</summary>
  public static MimeEntry Z2 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z3</summary>
  public static MimeEntry Z3 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z4</summary>
  public static MimeEntry Z4 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z5</summary>
  public static MimeEntry Z5 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z6</summary>
  public static MimeEntry Z6 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z7</summary>
  public static MimeEntry Z7 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>z8</summary>
  public static MimeEntry Z8 { get; } = new(["z1", "z2", "z3", "z4", "z5", "z6", "z7", "z8"], "application/x-zmachine");
  ///<summary>xaml</summary>
  public static MimeEntry Xaml { get; } = new(["xaml"], "application/xaml+xml");
  ///<summary>xdf</summary>
  public static MimeEntry Xdf { get; } = new(["xdf"], "application/xcap-diff+xml");
  ///<summary>xenc</summary>
  public static MimeEntry Xenc { get; } = new(["xenc"], "application/xenc+xml");
  ///<summary>xhtml</summary>
  public static MimeEntry Xhtml { get; } = new(["xhtml", "xht"], "application/xhtml+xml");
  ///<summary>xht</summary>
  public static MimeEntry Xht { get; } = new(["xhtml", "xht"], "application/xhtml+xml");
  ///<summary>xml</summary>
  public static MimeEntry Xml { get; } = new(["xml", "xsl", "xsd", "rng"], "application/xml");
  ///<summary>xsl</summary>
  public static MimeEntry Xsl { get; } = new(["xml", "xsl", "xsd", "rng"], "application/xml");
  ///<summary>dtd</summary>
  public static MimeEntry Dtd { get; } = new(["dtd"], "application/xml-dtd");
  ///<summary>xop</summary>
  public static MimeEntry Xop { get; } = new(["xop"], "application/xop+xml");
  ///<summary>xpl</summary>
  public static MimeEntry Xpl { get; } = new(["xpl"], "application/xproc+xml");
  ///<summary>xslt</summary>
  public static MimeEntry Xslt { get; } = new(["xslt"], "application/xslt+xml");
  ///<summary>xspf</summary>
  public static MimeEntry Xspf { get; } = new(["xspf"], "application/xspf+xml");
  ///<summary>mxml</summary>
  public static MimeEntry Mxml { get; } = new(["mxml", "xhvml", "xvml", "xvm"], "application/xv+xml");
  ///<summary>xhvml</summary>
  public static MimeEntry Xhvml { get; } = new(["mxml", "xhvml", "xvml", "xvm"], "application/xv+xml");
  ///<summary>xvml</summary>
  public static MimeEntry Xvml { get; } = new(["mxml", "xhvml", "xvml", "xvm"], "application/xv+xml");
  ///<summary>xvm</summary>
  public static MimeEntry Xvm { get; } = new(["mxml", "xhvml", "xvml", "xvm"], "application/xv+xml");
  ///<summary>yang</summary>
  public static MimeEntry Yang { get; } = new(["yang"], "application/yang");
  ///<summary>yin</summary>
  public static MimeEntry Yin { get; } = new(["yin"], "application/yin+xml");
  ///<summary>zip</summary>
  public static MimeEntry Zip { get; } = new(["zip"], "application/zip");
  ///<summary>adp</summary>
  public static MimeEntry Adp { get; } = new(["adp"], "audio/adpcm");
  ///<summary>au</summary>
  public static MimeEntry Au { get; } = new(["au", "snd"], "audio/basic");
  ///<summary>snd</summary>
  public static MimeEntry Snd { get; } = new(["au", "snd"], "audio/basic");
  ///<summary>mid</summary>
  public static MimeEntry Mid { get; } = new(["mid", "midi", "kar", "rmi"], "audio/midi");
  ///<summary>midi</summary>
  public static MimeEntry Midi { get; } = new(["mid", "midi", "kar", "rmi"], "audio/midi");
  ///<summary>kar</summary>
  public static MimeEntry Kar { get; } = new(["mid", "midi", "kar", "rmi"], "audio/midi");
  ///<summary>rmi</summary>
  public static MimeEntry Rmi { get; } = new(["mid", "midi", "kar", "rmi"], "audio/midi");
  ///<summary>m4a</summary>
  public static MimeEntry M4a { get; } = new(["m4a", "mp4a"], "audio/mp4");
  ///<summary>mp4a</summary>
  public static MimeEntry Mp4a { get; } = new(["m4a", "mp4a"], "audio/mp4");
  ///<summary>mpga</summary>
  public static MimeEntry Mpga { get; } = new(["mpga", "mp2", "mp2a", "m2a", "m3a"], "audio/mpeg");
  ///<summary>mp2</summary>
  public static MimeEntry Mp2 { get; } = new(["mpga", "mp2", "mp2a", "m2a", "m3a"], "audio/mpeg");
  ///<summary>mp2a</summary>
  public static MimeEntry Mp2a { get; } = new(["mpga", "mp2", "mp2a", "m2a", "m3a"], "audio/mpeg");
  ///<summary>mp3</summary>
  public static MimeEntry Mp3 { get; } = new(["mpga", "mp2", "mp2a", "m2a", "m3a"], "audio/mpeg");
  ///<summary>m2a</summary>
  public static MimeEntry M2a { get; } = new(["mpga", "mp2", "mp2a", "m2a", "m3a"], "audio/mpeg");
  ///<summary>m3a</summary>
  public static MimeEntry M3a { get; } = new(["mpga", "mp2", "mp2a", "m2a", "m3a"], "audio/mpeg");
  ///<summary>oga</summary>
  public static MimeEntry Oga { get; } = new(["oga", "ogg", "spx", "opus"], "audio/ogg");
  ///<summary>ogg</summary>
  public static MimeEntry Ogg { get; } = new(["oga", "ogg", "spx", "opus"], "audio/ogg");
  ///<summary>spx</summary>
  public static MimeEntry Spx { get; } = new(["oga", "ogg", "spx", "opus"], "audio/ogg");
  ///<summary>opus</summary>
  public static MimeEntry Opus { get; } = new(["oga", "ogg", "spx", "opus"], "audio/ogg");
  ///<summary>s3m</summary>
  public static MimeEntry S3m { get; } = new(["s3m"], "audio/s3m");
  ///<summary>sil</summary>
  public static MimeEntry Sil { get; } = new(["sil"], "audio/silk");
  ///<summary>uva</summary>
  public static MimeEntry Uva { get; } = new(["uva", "uvva"], "audio/vnd.dece.audio");
  ///<summary>uvva</summary>
  public static MimeEntry Uvva { get; } = new(["uva", "uvva"], "audio/vnd.dece.audio");
  ///<summary>eol</summary>
  public static MimeEntry Eol { get; } = new(["eol"], "audio/vnd.digital-winds");
  ///<summary>dra</summary>
  public static MimeEntry Dra { get; } = new(["dra"], "audio/vnd.dra");
  ///<summary>dts</summary>
  public static MimeEntry Dts { get; } = new(["dts"], "audio/vnd.dts");
  ///<summary>dtshd</summary>
  public static MimeEntry Dtshd { get; } = new(["dtshd"], "audio/vnd.dts.hd");
  ///<summary>lvp</summary>
  public static MimeEntry Lvp { get; } = new(["lvp"], "audio/vnd.lucent.voice");
  ///<summary>pya</summary>
  public static MimeEntry Pya { get; } = new(["pya"], "audio/vnd.ms-playready.media.pya");
  ///<summary>ecelp4800</summary>
  public static MimeEntry Ecelp4800 { get; } = new(["ecelp4800"], "audio/vnd.nuera.ecelp4800");
  ///<summary>ecelp7470</summary>
  public static MimeEntry Ecelp7470 { get; } = new(["ecelp7470"], "audio/vnd.nuera.ecelp7470");
  ///<summary>ecelp9600</summary>
  public static MimeEntry Ecelp9600 { get; } = new(["ecelp9600"], "audio/vnd.nuera.ecelp9600");
  ///<summary>rip</summary>
  public static MimeEntry Rip { get; } = new(["rip"], "audio/vnd.rip");
  ///<summary>weba</summary>
  public static MimeEntry Weba { get; } = new(["weba"], "audio/webm");
  ///<summary>aac</summary>
  public static MimeEntry Aac { get; } = new(["aac"], "audio/x-aac");
  ///<summary>aif</summary>
  public static MimeEntry Aif { get; } = new(["aif", "aiff", "aifc"], "audio/x-aiff");
  ///<summary>aiff</summary>
  public static MimeEntry Aiff { get; } = new(["aif", "aiff", "aifc"], "audio/x-aiff");
  ///<summary>aifc</summary>
  public static MimeEntry Aifc { get; } = new(["aif", "aiff", "aifc"], "audio/x-aiff");
  ///<summary>caf</summary>
  public static MimeEntry Caf { get; } = new(["caf"], "audio/x-caf");
  ///<summary>flac</summary>
  public static MimeEntry Flac { get; } = new(["flac"], "audio/x-flac");
  ///<summary>mka</summary>
  public static MimeEntry Mka { get; } = new(["mka"], "audio/x-matroska");
  ///<summary>m3u</summary>
  public static MimeEntry M3u { get; } = new(["m3u"], "audio/x-mpegurl");
  ///<summary>wax</summary>
  public static MimeEntry Wax { get; } = new(["wax"], "audio/x-ms-wax");
  ///<summary>wma</summary>
  public static MimeEntry Wma { get; } = new(["wma"], "audio/x-ms-wma");
  ///<summary>ram</summary>
  public static MimeEntry Ram { get; } = new(["ram", "ra"], "audio/x-pn-realaudio");
  ///<summary>ra</summary>
  public static MimeEntry Ra { get; } = new(["ram", "ra"], "audio/x-pn-realaudio");
  ///<summary>rmp</summary>
  public static MimeEntry Rmp { get; } = new(["rmp"], "audio/x-pn-realaudio-plugin");
  ///<summary>wav</summary>
  public static MimeEntry Wav { get; } = new(["wav"], "audio/x-wav");
  ///<summary>xm</summary>
  public static MimeEntry Xm { get; } = new(["xm"], "audio/xm");
  ///<summary>cdx</summary>
  public static MimeEntry Cdx { get; } = new(["cdx"], "chemical/x-cdx");
  ///<summary>cif</summary>
  public static MimeEntry Cif { get; } = new(["cif"], "chemical/x-cif");
  ///<summary>cmdf</summary>
  public static MimeEntry Cmdf { get; } = new(["cmdf"], "chemical/x-cmdf");
  ///<summary>cml</summary>
  public static MimeEntry Cml { get; } = new(["cml"], "chemical/x-cml");
  ///<summary>csml</summary>
  public static MimeEntry Csml { get; } = new(["csml"], "chemical/x-csml");
  ///<summary>xyz</summary>
  public static MimeEntry Xyz { get; } = new(["xyz"], "chemical/x-xyz");
  ///<summary>ttc</summary>
  public static MimeEntry Ttc { get; } = new(["ttc"], "font/collection");
  ///<summary>otf</summary>
  public static MimeEntry Otf { get; } = new(["otf"], "font/otf");
  ///<summary>ttf</summary>
  public static MimeEntry Ttf { get; } = new(["ttf"], "font/ttf");
  ///<summary>woff</summary>
  public static MimeEntry Woff { get; } = new(["woff"], "font/woff");
  ///<summary>woff2</summary>
  public static MimeEntry Woff2 { get; } = new(["woff2"], "font/woff2");
  ///<summary>bmp</summary>
  public static MimeEntry Bmp { get; } = new(["bmp", "dib"], "image/bmp");
  ///<summary>cgm</summary>
  public static MimeEntry Cgm { get; } = new(["cgm"], "image/cgm");
  ///<summary>g3</summary>
  public static MimeEntry G3 { get; } = new(["g3"], "image/g3fax");
  ///<summary>gif</summary>
  public static MimeEntry Gif { get; } = new(["gif"], "image/gif");
  ///<summary>ief</summary>
  public static MimeEntry Ief { get; } = new(["ief"], "image/ief");
  ///<summary>jpeg</summary>
  public static MimeEntry Jpeg { get; } = new(["jpeg", "jpg", "jpe"], "image/jpeg");
  ///<summary>jpg</summary>
  public static MimeEntry Jpg { get; } = new(["jpeg", "jpg", "jpe"], "image/jpeg");
  ///<summary>jpe</summary>
  public static MimeEntry Jpe { get; } = new(["jpeg", "jpg", "jpe"], "image/jpeg");
  ///<summary>ktx</summary>
  public static MimeEntry Ktx { get; } = new(["ktx"], "image/ktx");
  ///<summary>png</summary>
  public static MimeEntry Png { get; } = new(["png"], "image/png");
  ///<summary>btif</summary>
  public static MimeEntry Btif { get; } = new(["btif", "btf"], "image/prs.btif");
  ///<summary>sgi</summary>
  public static MimeEntry Sgi { get; } = new(["sgi"], "image/sgi");
  ///<summary>svg</summary>
  public static MimeEntry Svg { get; } = new(["svg", "svgz"], "image/svg+xml");
  ///<summary>svgz</summary>
  public static MimeEntry Svgz { get; } = new(["svg", "svgz"], "image/svg+xml");
  ///<summary>tiff</summary>
  public static MimeEntry Tiff { get; } = new(["tif", "tiff"], "image/tiff");
  ///<summary>tif</summary>
  public static MimeEntry Tif { get; } = new(["tif", "tiff"], "image/tiff");
  ///<summary>psd</summary>
  public static MimeEntry Psd { get; } = new(["psd"], "image/vnd.adobe.photoshop");
  ///<summary>uvi</summary>
  public static MimeEntry Uvi { get; } = new(["uvi", "uvvi", "uvg", "uvvg"], "image/vnd.dece.graphic");
  ///<summary>uvvi</summary>
  public static MimeEntry Uvvi { get; } = new(["uvi", "uvvi", "uvg", "uvvg"], "image/vnd.dece.graphic");
  ///<summary>uvg</summary>
  public static MimeEntry Uvg { get; } = new(["uvi", "uvvi", "uvg", "uvvg"], "image/vnd.dece.graphic");
  ///<summary>uvvg</summary>
  public static MimeEntry Uvvg { get; } = new(["uvi", "uvvi", "uvg", "uvvg"], "image/vnd.dece.graphic");
  ///<summary>djvu</summary>
  public static MimeEntry Djvu { get; } = new(["djvu", "djv"], "image/vnd.djvu");
  ///<summary>djv</summary>
  public static MimeEntry Djv { get; } = new(["djvu", "djv"], "image/vnd.djvu");
  ///<summary>sub</summary>
  public static MimeEntry Sub { get; } = new(["sub"], "image/vnd.dvb.subtitle");
  ///<summary>dwg</summary>
  public static MimeEntry Dwg { get; } = new(["dwg"], "image/vnd.dwg");
  ///<summary>dxf</summary>
  public static MimeEntry Dxf { get; } = new(["dxf"], "image/vnd.dxf");
  ///<summary>fbs</summary>
  public static MimeEntry Fbs { get; } = new(["fbs"], "image/vnd.fastbidsheet");
  ///<summary>fpx</summary>
  public static MimeEntry Fpx { get; } = new(["fpx"], "image/vnd.fpx");
  ///<summary>fst</summary>
  public static MimeEntry Fst { get; } = new(["fst"], "image/vnd.fst");
  ///<summary>mmr</summary>
  public static MimeEntry Mmr { get; } = new(["mmr"], "image/vnd.fujixerox.edmics-mmr");
  ///<summary>rlc</summary>
  public static MimeEntry Rlc { get; } = new(["rlc"], "image/vnd.fujixerox.edmics-rlc");
  ///<summary>mdi</summary>
  public static MimeEntry Mdi { get; } = new(["mdi"], "image/vnd.ms-modi");
  ///<summary>wdp</summary>
  public static MimeEntry Wdp { get; } = new(["wdp"], "image/vnd.ms-photo");
  ///<summary>npx</summary>
  public static MimeEntry Npx { get; } = new(["npx"], "image/vnd.net-fpx");
  ///<summary>wbmp</summary>
  public static MimeEntry Wbmp { get; } = new(["wbmp"], "image/vnd.wap.wbmp");
  ///<summary>xif</summary>
  public static MimeEntry Xif { get; } = new(["xif"], "image/vnd.xiff");
  ///<summary>webp</summary>
  public static MimeEntry Webp { get; } = new(["webp"], "image/webp");
  ///<summary>3ds</summary>
  public static MimeEntry _3ds { get; } = new(["3ds"], "image/x-3ds");
  ///<summary>ras</summary>
  public static MimeEntry Ras { get; } = new(["ras"], "image/x-cmu-raster");
  ///<summary>cmx</summary>
  public static MimeEntry Cmx { get; } = new(["cmx"], "image/x-cmx");
  ///<summary>fh</summary>
  public static MimeEntry Fh { get; } = new(["fh", "fhc", "fh4", "fh5", "fh7"], "image/x-freehand");
  ///<summary>fhc</summary>
  public static MimeEntry Fhc { get; } = new(["fh", "fhc", "fh4", "fh5", "fh7"], "image/x-freehand");
  ///<summary>fh4</summary>
  public static MimeEntry Fh4 { get; } = new(["fh", "fhc", "fh4", "fh5", "fh7"], "image/x-freehand");
  ///<summary>fh5</summary>
  public static MimeEntry Fh5 { get; } = new(["fh", "fhc", "fh4", "fh5", "fh7"], "image/x-freehand");
  ///<summary>fh7</summary>
  public static MimeEntry Fh7 { get; } = new(["fh", "fhc", "fh4", "fh5", "fh7"], "image/x-freehand");
  ///<summary>ico</summary>
  public static MimeEntry Ico { get; } = new(["ico"], "image/x-icon");
  ///<summary>sid</summary>
  public static MimeEntry Sid { get; } = new(["sid"], "image/x-mrsid-image");
  ///<summary>pcx</summary>
  public static MimeEntry Pcx { get; } = new(["pcx"], "image/x-pcx");
  ///<summary>pic</summary>
  public static MimeEntry Pic { get; } = new(["pic", "pct"], "image/x-pict");
  ///<summary>pct</summary>
  public static MimeEntry Pct { get; } = new(["pic", "pct"], "image/x-pict");
  ///<summary>pnm</summary>
  public static MimeEntry Pnm { get; } = new(["pnm"], "image/x-portable-anymap");
  ///<summary>pbm</summary>
  public static MimeEntry Pbm { get; } = new(["pbm"], "image/x-portable-bitmap");
  ///<summary>pgm</summary>
  public static MimeEntry Pgm { get; } = new(["pgm"], "image/x-portable-graymap");
  ///<summary>ppm</summary>
  public static MimeEntry Ppm { get; } = new(["ppm"], "image/x-portable-pixmap");
  ///<summary>rgb</summary>
  public static MimeEntry Rgb { get; } = new(["rgb"], "image/x-rgb");
  ///<summary>tga</summary>
  public static MimeEntry Tga { get; } = new(["tga"], "image/x-tga");
  ///<summary>xbm</summary>
  public static MimeEntry Xbm { get; } = new(["xbm"], "image/x-xbitmap");
  ///<summary>xpm</summary>
  public static MimeEntry Xpm { get; } = new(["xpm"], "image/x-xpixmap");
  ///<summary>xwd</summary>
  public static MimeEntry Xwd { get; } = new(["xwd"], "image/x-xwindowdump");
  ///<summary>eml</summary>
  public static MimeEntry Eml { get; } = new(["eml", "mime"], "message/rfc822");
  ///<summary>mime</summary>
  public static MimeEntry Mime { get; } = new(["eml", "mime"], "message/rfc822");
  ///<summary>igs</summary>
  public static MimeEntry Igs { get; } = new(["igs", "iges"], "model/iges");
  ///<summary>iges</summary>
  public static MimeEntry Iges { get; } = new(["igs", "iges"], "model/iges");
  ///<summary>msh</summary>
  public static MimeEntry Msh { get; } = new(["msh", "mesh", "silo"], "model/mesh");
  ///<summary>mesh</summary>
  public static MimeEntry Mesh { get; } = new(["msh", "mesh", "silo"], "model/mesh");
  ///<summary>silo</summary>
  public static MimeEntry Silo { get; } = new(["msh", "mesh", "silo"], "model/mesh");
  ///<summary>dae</summary>
  public static MimeEntry Dae { get; } = new(["dae"], "model/vnd.collada+xml");
  ///<summary>dwf</summary>
  public static MimeEntry Dwf { get; } = new(["dwf"], "model/vnd.dwf");
  ///<summary>gdl</summary>
  public static MimeEntry Gdl { get; } = new(["gdl"], "model/vnd.gdl");
  ///<summary>gtw</summary>
  public static MimeEntry Gtw { get; } = new(["gtw"], "model/vnd.gtw");
  ///<summary>mts</summary>
  public static MimeEntry Mts { get; } = new(["mts"], "model/vnd.mts");
  ///<summary>vtu</summary>
  public static MimeEntry Vtu { get; } = new(["vtu"], "model/vnd.vtu");
  ///<summary>wrl</summary>
  public static MimeEntry Wrl { get; } = new(["wrl", "vrml"], "model/vrml");
  ///<summary>vrml</summary>
  public static MimeEntry Vrml { get; } = new(["wrl", "vrml"], "model/vrml");
  ///<summary>x3db</summary>
  public static MimeEntry X3db { get; } = new(["x3db", "x3dbz"], "model/x3d+binary");
  ///<summary>x3dbz</summary>
  public static MimeEntry X3dbz { get; } = new(["x3db", "x3dbz"], "model/x3d+binary");
  ///<summary>x3dv</summary>
  public static MimeEntry X3dv { get; } = new(["x3dv", "x3dvz"], "model/x3d+vrml");
  ///<summary>x3dvz</summary>
  public static MimeEntry X3dvz { get; } = new(["x3dv", "x3dvz"], "model/x3d+vrml");
  ///<summary>x3d</summary>
  public static MimeEntry X3d { get; } = new(["x3d", "x3dz"], "model/x3d+xml");
  ///<summary>x3dz</summary>
  public static MimeEntry X3dz { get; } = new(["x3d", "x3dz"], "model/x3d+xml");
  ///<summary>appcache</summary>
  public static MimeEntry Appcache { get; } = new(["appcache", "manifest"], "text/cache-manifest");
  ///<summary>ics</summary>
  public static MimeEntry Ics { get; } = new(["ics", "ifb"], "text/calendar");
  ///<summary>ifb</summary>
  public static MimeEntry Ifb { get; } = new(["ics", "ifb"], "text/calendar");
  ///<summary>css</summary>
  public static MimeEntry Css { get; } = new(["css"], "text/css");
  ///<summary>csv</summary>
  public static MimeEntry Csv { get; } = new(["csv"], "text/csv");
  ///<summary>html</summary>
  public static MimeEntry Html { get; } = new(["html", "htm", "shtml"], "text/html");
  ///<summary>htm</summary>
  public static MimeEntry Htm { get; } = new(["html", "htm", "shtml"], "text/html");
  ///<summary>n3</summary>
  public static MimeEntry N3 { get; } = new(["n3"], "text/n3");
  ///<summary>txt</summary>
  public static MimeEntry Txt { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>text</summary>
  public static MimeEntry Text { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>conf</summary>
  public static MimeEntry Conf { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>def</summary>
  public static MimeEntry Def { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>list</summary>
  public static MimeEntry List { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>log</summary>
  public static MimeEntry Log { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>in</summary>
  public static MimeEntry In { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>dsc</summary>
  public static MimeEntry Dsc { get; } = new(["dsc"], "text/prs.lines.tag");
  ///<summary>rtx</summary>
  public static MimeEntry Rtx { get; } = new(["rtx"], "text/richtext");
  ///<summary>sgml</summary>
  public static MimeEntry Sgml { get; } = new(["sgml", "sgm"], "text/sgml");
  ///<summary>sgm</summary>
  public static MimeEntry Sgm { get; } = new(["sgml", "sgm"], "text/sgml");
  ///<summary>tsv</summary>
  public static MimeEntry Tsv { get; } = new(["tsv"], "text/tab-separated-values");
  ///<summary>t</summary>
  public static MimeEntry T { get; } = new(["t", "tr", "roff", "man", "me", "ms"], "text/troff");
  ///<summary>tr</summary>
  public static MimeEntry Tr { get; } = new(["t", "tr", "roff", "man", "me", "ms"], "text/troff");
  ///<summary>roff</summary>
  public static MimeEntry Roff { get; } = new(["t", "tr", "roff", "man", "me", "ms"], "text/troff");
  ///<summary>man</summary>
  public static MimeEntry Man { get; } = new(["t", "tr", "roff", "man", "me", "ms"], "text/troff");
  ///<summary>me</summary>
  public static MimeEntry Me { get; } = new(["t", "tr", "roff", "man", "me", "ms"], "text/troff");
  ///<summary>ms</summary>
  public static MimeEntry Ms { get; } = new(["t", "tr", "roff", "man", "me", "ms"], "text/troff");
  ///<summary>ttl</summary>
  public static MimeEntry Ttl { get; } = new(["ttl"], "text/turtle");
  ///<summary>uri</summary>
  public static MimeEntry Uri { get; } = new(["uri", "uris", "urls"], "text/uri-list");
  ///<summary>uris</summary>
  public static MimeEntry Uris { get; } = new(["uri", "uris", "urls"], "text/uri-list");
  ///<summary>urls</summary>
  public static MimeEntry Urls { get; } = new(["uri", "uris", "urls"], "text/uri-list");
  ///<summary>vcard</summary>
  public static MimeEntry Vcard { get; } = new(["vcard"], "text/vcard");
  ///<summary>curl</summary>
  public static MimeEntry Curl { get; } = new(["curl"], "text/vnd.curl");
  ///<summary>dcurl</summary>
  public static MimeEntry Dcurl { get; } = new(["dcurl"], "text/vnd.curl.dcurl");
  ///<summary>mcurl</summary>
  public static MimeEntry Mcurl { get; } = new(["mcurl"], "text/vnd.curl.mcurl");
  ///<summary>scurl</summary>
  public static MimeEntry Scurl { get; } = new(["scurl"], "text/vnd.curl.scurl");
  ///<summary>fly</summary>
  public static MimeEntry Fly { get; } = new(["fly"], "text/vnd.fly");
  ///<summary>flx</summary>
  public static MimeEntry Flx { get; } = new(["flx"], "text/vnd.fmi.flexstor");
  ///<summary>gv</summary>
  public static MimeEntry Gv { get; } = new(["gv"], "text/vnd.graphviz");
  ///<summary>3dml</summary>
  public static MimeEntry _3dml { get; } = new(["3dml"], "text/vnd.in3d.3dml");
  ///<summary>spot</summary>
  public static MimeEntry Spot { get; } = new(["spot"], "text/vnd.in3d.spot");
  ///<summary>jad</summary>
  public static MimeEntry Jad { get; } = new(["jad"], "text/vnd.sun.j2me.app-descriptor");
  ///<summary>wml</summary>
  public static MimeEntry Wml { get; } = new(["wml"], "text/vnd.wap.wml");
  ///<summary>wmls</summary>
  public static MimeEntry Wmls { get; } = new(["wmls"], "text/vnd.wap.wmlscript");
  ///<summary>s</summary>
  public static MimeEntry S { get; } = new(["s", "asm"], "text/x-asm");
  ///<summary>asm</summary>
  public static MimeEntry Asm { get; } = new(["s", "asm"], "text/x-asm");
  ///<summary>c</summary>
  public static MimeEntry C { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>cc</summary>
  public static MimeEntry Cc { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>cxx</summary>
  public static MimeEntry Cxx { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>cpp</summary>
  public static MimeEntry Cpp { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>h</summary>
  public static MimeEntry H { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>hh</summary>
  public static MimeEntry Hh { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>dic</summary>
  public static MimeEntry Dic { get; } = new(["c", "cc", "cxx", "cpp", "h", "hh", "dic"], "text/x-c");
  ///<summary>f</summary>
  public static MimeEntry F { get; } = new(["f", "for", "f77", "f90"], "text/x-fortran");
  ///<summary>for</summary>
  public static MimeEntry For { get; } = new(["f", "for", "f77", "f90"], "text/x-fortran");
  ///<summary>f77</summary>
  public static MimeEntry F77 { get; } = new(["f", "for", "f77", "f90"], "text/x-fortran");
  ///<summary>f90</summary>
  public static MimeEntry F90 { get; } = new(["f", "for", "f77", "f90"], "text/x-fortran");
  ///<summary>java</summary>
  public static MimeEntry Java { get; } = new(["java"], "text/x-java-source");
  ///<summary>nfo</summary>
  public static MimeEntry Nfo { get; } = new(["nfo"], "text/x-nfo");
  ///<summary>opml</summary>
  public static MimeEntry Opml { get; } = new(["opml"], "text/x-opml");
  ///<summary>p</summary>
  public static MimeEntry P { get; } = new(["p", "pas"], "text/x-pascal");
  ///<summary>pas</summary>
  public static MimeEntry Pas { get; } = new(["p", "pas"], "text/x-pascal");
  ///<summary>etx</summary>
  public static MimeEntry Etx { get; } = new(["etx"], "text/x-setext");
  ///<summary>sfv</summary>
  public static MimeEntry Sfv { get; } = new(["sfv"], "text/x-sfv");
  ///<summary>uu</summary>
  public static MimeEntry Uu { get; } = new(["uu"], "text/x-uuencode");
  ///<summary>vcs</summary>
  public static MimeEntry Vcs { get; } = new(["vcs"], "text/x-vcalendar");
  ///<summary>vcf</summary>
  public static MimeEntry Vcf { get; } = new(["vcf"], "text/x-vcard");
  ///<summary>3gp</summary>
  public static MimeEntry _3gp { get; } = new(["3gp"], "video/3gpp");
  ///<summary>3g2</summary>
  public static MimeEntry _3g2 { get; } = new(["3g2"], "video/3gpp2");
  ///<summary>h261</summary>
  public static MimeEntry H261 { get; } = new(["h261"], "video/h261");
  ///<summary>h263</summary>
  public static MimeEntry H263 { get; } = new(["h263"], "video/h263");
  ///<summary>h264</summary>
  public static MimeEntry H264 { get; } = new(["h264"], "video/h264");
  ///<summary>jpgv</summary>
  public static MimeEntry Jpgv { get; } = new(["jpgv"], "video/jpeg");
  ///<summary>jpm</summary>
  public static MimeEntry Jpm { get; } = new(["jpm"], "video/jpm");
  ///<summary>jpgm</summary>
  public static MimeEntry Jpgm { get; } = new(["jpgm"], "video/jpm");
  ///<summary>mj2</summary>
  public static MimeEntry Mj2 { get; } = new(["mj2", "mjp2"], "video/mj2");
  ///<summary>mjp2</summary>
  public static MimeEntry Mjp2 { get; } = new(["mj2", "mjp2"], "video/mj2");
  ///<summary>mp4</summary>
  public static MimeEntry Mp4 { get; } = new(["mp4v"], "video/mp4");
  ///<summary>mp4v</summary>
  public static MimeEntry Mp4v { get; } = new(["mp4v"], "video/mp4");
  ///<summary>mpg4</summary>
  public static MimeEntry Mpg4 { get; } = new(["mp4v"], "video/mp4");
  ///<summary>mpeg</summary>
  public static MimeEntry Mpeg { get; } = new(["mpeg", "mpg", "mpe", "m1v", "m2v"], "video/mpeg");
  ///<summary>mpg</summary>
  public static MimeEntry Mpg { get; } = new(["mpeg", "mpg", "mpe", "m1v", "m2v"], "video/mpeg");
  ///<summary>mpe</summary>
  public static MimeEntry Mpe { get; } = new(["mpeg", "mpg", "mpe", "m1v", "m2v"], "video/mpeg");
  ///<summary>m1v</summary>
  public static MimeEntry M1v { get; } = new(["mpeg", "mpg", "mpe", "m1v", "m2v"], "video/mpeg");
  ///<summary>m2v</summary>
  public static MimeEntry M2v { get; } = new(["mpeg", "mpg", "mpe", "m1v", "m2v"], "video/mpeg");
  ///<summary>ogv</summary>
  public static MimeEntry Ogv { get; } = new(["ogv"], "video/ogg");
  ///<summary>qt</summary>
  public static MimeEntry Qt { get; } = new(["qt", "mov"], "video/quicktime");
  ///<summary>mov</summary>
  public static MimeEntry Mov { get; } = new(["qt", "mov"], "video/quicktime");
  ///<summary>uvh</summary>
  public static MimeEntry Uvh { get; } = new(["uvh", "uvvh"], "video/vnd.dece.hd");
  ///<summary>uvvh</summary>
  public static MimeEntry Uvvh { get; } = new(["uvh", "uvvh"], "video/vnd.dece.hd");
  ///<summary>uvm</summary>
  public static MimeEntry Uvm { get; } = new(["uvm", "uvvm"], "video/vnd.dece.mobile");
  ///<summary>uvvm</summary>
  public static MimeEntry Uvvm { get; } = new(["uvm", "uvvm"], "video/vnd.dece.mobile");
  ///<summary>uvp</summary>
  public static MimeEntry Uvp { get; } = new(["uvp", "uvvp"], "video/vnd.dece.pd");
  ///<summary>uvvp</summary>
  public static MimeEntry Uvvp { get; } = new(["uvp", "uvvp"], "video/vnd.dece.pd");
  ///<summary>uvs</summary>
  public static MimeEntry Uvs { get; } = new(["uvs", "uvvs"], "video/vnd.dece.sd");
  ///<summary>uvvs</summary>
  public static MimeEntry Uvvs { get; } = new(["uvs", "uvvs"], "video/vnd.dece.sd");
  ///<summary>uvv</summary>
  public static MimeEntry Uvv { get; } = new(["uvv", "uvvv"], "video/vnd.dece.video");
  ///<summary>uvvv</summary>
  public static MimeEntry Uvvv { get; } = new(["uvv", "uvvv"], "video/vnd.dece.video");
  ///<summary>dvb</summary>
  public static MimeEntry Dvb { get; } = new(["dvb"], "video/vnd.dvb.file");
  ///<summary>fvt</summary>
  public static MimeEntry Fvt { get; } = new(["fvt"], "video/vnd.fvt");
  ///<summary>mxu</summary>
  public static MimeEntry Mxu { get; } = new(["mxu", "m4u"], "video/vnd.mpegurl");
  ///<summary>m4u</summary>
  public static MimeEntry M4u { get; } = new(["mxu", "m4u"], "video/vnd.mpegurl");
  ///<summary>pyv</summary>
  public static MimeEntry Pyv { get; } = new(["pyv"], "video/vnd.ms-playready.media.pyv");
  ///<summary>uvu</summary>
  public static MimeEntry Uvu { get; } = new(["uvu", "uvvu"], "video/vnd.uvvu.mp4");
  ///<summary>uvvu</summary>
  public static MimeEntry Uvvu { get; } = new(["uvu", "uvvu"], "video/vnd.uvvu.mp4");
  ///<summary>viv</summary>
  public static MimeEntry Viv { get; } = new(["viv"], "video/vnd.vivo");
  ///<summary>webm</summary>
  public static MimeEntry Webm { get; } = new(["webm"], "video/webm");
  ///<summary>f4v</summary>
  public static MimeEntry F4v { get; } = new(["f4v"], "video/x-f4v");
  ///<summary>fli</summary>
  public static MimeEntry Fli { get; } = new(["fli"], "video/x-fli");
  ///<summary>flv</summary>
  public static MimeEntry Flv { get; } = new(["flv"], "video/x-flv");
  ///<summary>m4v</summary>
  public static MimeEntry M4v { get; } = new(["m4v"], "video/x-m4v");
  ///<summary>mkv</summary>
  public static MimeEntry Mkv { get; } = new(["mkv", "mk3d", "mks"], "video/x-matroska");
  ///<summary>mk3d</summary>
  public static MimeEntry Mk3d { get; } = new(["mkv", "mk3d", "mks"], "video/x-matroska");
  ///<summary>mks</summary>
  public static MimeEntry Mks { get; } = new(["mkv", "mk3d", "mks"], "video/x-matroska");
  ///<summary>mng</summary>
  public static MimeEntry Mng { get; } = new(["mng"], "video/x-mng");
  ///<summary>asf</summary>
  public static MimeEntry Asf { get; } = new(["asf", "asx"], "video/x-ms-asf");
  ///<summary>asx</summary>
  public static MimeEntry Asx { get; } = new(["asf", "asx"], "video/x-ms-asf");
  ///<summary>vob</summary>
  public static MimeEntry Vob { get; } = new(["vob"], "video/x-ms-vob");
  ///<summary>wm</summary>
  public static MimeEntry Wm { get; } = new(["wm"], "video/x-ms-wm");
  ///<summary>wmv</summary>
  public static MimeEntry Wmv { get; } = new(["wmv"], "video/x-ms-wmv");
  ///<summary>wmx</summary>
  public static MimeEntry Wmx { get; } = new(["wmx"], "video/x-ms-wmx");
  ///<summary>wvx</summary>
  public static MimeEntry Wvx { get; } = new(["wvx"], "video/x-ms-wvx");
  ///<summary>avi</summary>
  public static MimeEntry Avi { get; } = new(["avi"], "video/x-msvideo");
  ///<summary>movie</summary>
  public static MimeEntry Movie { get; } = new(["movie"], "video/x-sgi-movie");
  ///<summary>smv</summary>
  public static MimeEntry Smv { get; } = new(["smv"], "video/x-smv");
  ///<summary>ice</summary>
  public static MimeEntry Ice { get; } = new(["ice"], "x-conference/x-cooltalk");
  ///<summary>map</summary>
  public static MimeEntry Map { get; } = new(["json", "map"], "application/json");
  ///<summary>topojson</summary>
  public static MimeEntry Topojson { get; } = new(["json", "map"], "application/json");
  ///<summary>jsonld</summary>
  public static MimeEntry Jsonld { get; } = new(["jsonld"], "application/ld+json");
  ///<summary>geojson</summary>
  public static MimeEntry Geojson { get; } = new(["geojson"], "application/geo+json");
  ///<summary>mjs</summary>
  public static MimeEntry Mjs { get; } = new(["mjs"], "text/javascript");
  ///<summary>wasm</summary>
  public static MimeEntry Wasm { get; } = new(["wasm"], "application/wasm");
  ///<summary>webmanifest</summary>
  public static MimeEntry Webmanifest { get; } = new(["webmanifest"], "application/manifest+json");
  ///<summary>webapp</summary>
  public static MimeEntry Webapp { get; } = new(["webapp"], "application/x-web-app-manifest+json");
  ///<summary>f4a</summary>
  public static MimeEntry F4a { get; } = new(["m4a", "mp4a"], "audio/mp4");
  ///<summary>f4b</summary>
  public static MimeEntry F4b { get; } = new(["m4a", "mp4a"], "audio/mp4");
  ///<summary>apng</summary>
  public static MimeEntry Apng { get; } = new(["apng"], "image/apng");
  ///<summary>avif</summary>
  public static MimeEntry Avif { get; } = new(["avif"], "image/avif");
  ///<summary>avifs</summary>
  public static MimeEntry Avifs { get; } = new(["avifs"], "image/avif-sequence");
  ///<summary>jxr</summary>
  public static MimeEntry Jxr { get; } = new(["jxr"], "image/jxr");
  ///<summary>hdp</summary>
  public static MimeEntry Hdp { get; } = new(["jxr"], "image/jxr");
  ///<summary>jng</summary>
  public static MimeEntry Jng { get; } = new(["jng"], "image/x-jng");
  ///<summary>3gpp</summary>
  public static MimeEntry _3gpp { get; } = new(["3gp"], "video/3gpp");
  ///<summary>f4p</summary>
  public static MimeEntry F4p { get; } = new(["mp4v"], "video/mp4");
  ///<summary>cur</summary>
  public static MimeEntry Cur { get; } = new(["cur"], "image/x-icon");
  ///<summary>ear</summary>
  public static MimeEntry Ear { get; } = new(["jar", "war", "ear"], "application/java-archive");
  ///<summary>war</summary>
  public static MimeEntry War { get; } = new(["jar", "war", "ear"], "application/java-archive");
  ///<summary>img</summary>
  public static MimeEntry Img { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>msm</summary>
  public static MimeEntry Msm { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>msp</summary>
  public static MimeEntry Msp { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>safariextz</summary>
  public static MimeEntry Safariextz { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>bbaw</summary>
  public static MimeEntry Bbaw { get; } = new(["bbaw"], "application/x-bb-appworld");
  ///<summary>crx</summary>
  public static MimeEntry Crx { get; } = new(["crx"], "application/x-chrome-extension");
  ///<summary>cco</summary>
  public static MimeEntry Cco { get; } = new(["cco"], "application/x-cocoa");
  ///<summary>jardiff</summary>
  public static MimeEntry Jardiff { get; } = new(["jardiff"], "application/x-java-archive-diff");
  ///<summary>run</summary>
  public static MimeEntry Run { get; } = new(["run"], "application/x-makeself");
  ///<summary>oex</summary>
  public static MimeEntry Oex { get; } = new(["oex"], "application/x-opera-extension");
  ///<summary>pl</summary>
  public static MimeEntry Pl { get; } = new(["pl", "pm"], "application/x-perl");
  ///<summary>pm</summary>
  public static MimeEntry Pm { get; } = new(["pl", "pm"], "application/x-perl");
  ///<summary>rpm</summary>
  public static MimeEntry Rpm { get; } = new(["rpm"], "application/x-redhat-package-manager");
  ///<summary>sea</summary>
  public static MimeEntry Sea { get; } = new(["sea"], "application/x-sea");
  ///<summary>tk</summary>
  public static MimeEntry Tk { get; } = new(["tcl", "tk"], "application/x-tcl");
  ///<summary>pem</summary>
  public static MimeEntry Pem { get; } = new(["der", "crt", "pem"], "application/x-x509-ca-cert");
  ///<summary>shtml</summary>
  public static MimeEntry Shtml { get; } = new(["html", "htm", "shtml"], "text/html");
  ///<summary>md</summary>
  public static MimeEntry Md { get; } = new(["md", "markdown"], "text/markdown");
  ///<summary>markdown</summary>
  public static MimeEntry Markdown { get; } = new(["md", "markdown"], "text/markdown");
  ///<summary>mml</summary>
  public static MimeEntry Mml { get; } = new(["mml"], "text/mathml");
  ///<summary>xloc</summary>
  public static MimeEntry Xloc { get; } = new(["xloc"], "text/vnd.rim.location.xloc");
  ///<summary>vtt</summary>
  public static MimeEntry Vtt { get; } = new(["vtt"], "text/vtt");
  ///<summary>htc</summary>
  public static MimeEntry Htc { get; } = new(["htc"], "text/x-component");
  ///<summary>bdoc</summary>
  public static MimeEntry Bdoc { get; } = new(["bdoc"], "application/bdoc");
  ///<summary>es</summary>
  public static MimeEntry Es { get; } = new(["ecma"], "application/ecmascript");
  ///<summary>hjson</summary>
  public static MimeEntry Hjson { get; } = new(["hjson"], "application/hjson");
  ///<summary>json5</summary>
  public static MimeEntry Json5 { get; } = new(["json5"], "application/json5");
  ///<summary>m4p</summary>
  public static MimeEntry M4p { get; } = new(["mp4", "mpg4", "mp4s", "m4p"], "application/mp4");
  ///<summary>cjs</summary>
  public static MimeEntry Cjs { get; } = new(["cjs"], "application/node");
  ///<summary>buffer</summary>
  public static MimeEntry Buffer { get; } = new(["bin", "dms", "lrf", "mar", "so", "dist", "distz", "pkg", "bpk", "dump", "elc", "deploy", "exe", "dll", "deb", "dmg", "iso", "img", "msi", "msp", "msm", "buffer"], "application/octet-stream");
  ///<summary>raml</summary>
  public static MimeEntry Raml { get; } = new(["raml"], "application/raml+yaml");
  ///<summary>owl</summary>
  public static MimeEntry Owl { get; } = new(["rdf", "owl"], "application/rdf+xml");
  ///<summary>siv</summary>
  public static MimeEntry Siv { get; } = new(["siv", "sieve"], "application/sieve");
  ///<summary>sieve</summary>
  public static MimeEntry Sieve { get; } = new(["siv", "sieve"], "application/sieve");
  ///<summary>toml</summary>
  public static MimeEntry Toml { get; } = new(["toml"], "application/toml");
  ///<summary>ubj</summary>
  public static MimeEntry Ubj { get; } = new(["ubj"], "application/ubjson");
  ///<summary>pkpass</summary>
  public static MimeEntry Pkpass { get; } = new(["pkpass"], "application/vnd.apple.pkpass");
  ///<summary>gdoc</summary>
  public static MimeEntry Gdoc { get; } = new(["gdoc"], "application/vnd.google-apps.document");
  ///<summary>gslides</summary>
  public static MimeEntry Gslides { get; } = new(["gslides"], "application/vnd.google-apps.presentation");
  ///<summary>gsheet</summary>
  public static MimeEntry Gsheet { get; } = new(["gsheet"], "application/vnd.google-apps.spreadsheet");
  ///<summary>msg</summary>
  public static MimeEntry Msg { get; } = new(["msg"], "application/vnd.ms-outlook");
  ///<summary>arj</summary>
  public static MimeEntry Arj { get; } = new(["arj"], "application/x-arj");
  ///<summary>php</summary>
  public static MimeEntry Php { get; } = new(["php"], "application/x-httpd-php");
  ///<summary>kdbx</summary>
  public static MimeEntry Kdbx { get; } = new(["kdbx"], "application/x-keepass2");
  ///<summary>luac</summary>
  public static MimeEntry Luac { get; } = new(["luac"], "application/x-lua-bytecode");
  ///<summary>pac</summary>
  public static MimeEntry Pac { get; } = new(["pac"], "application/x-ns-proxy-autoconfig");
  ///<summary>hdd</summary>
  public static MimeEntry Hdd { get; } = new(["hdd"], "application/x-virtualbox-hdd");
  ///<summary>ova</summary>
  public static MimeEntry Ova { get; } = new(["ova"], "application/x-virtualbox-ova");
  ///<summary>ovf</summary>
  public static MimeEntry Ovf { get; } = new(["ovf"], "application/x-virtualbox-ovf");
  ///<summary>vbox</summary>
  public static MimeEntry Vbox { get; } = new(["vbox"], "application/x-virtualbox-vbox");
  ///<summary>vbox-extpack</summary>
  public static MimeEntry Vboxextpack { get; } = new(["vbox-extpack"], "application/x-virtualbox-vbox-extpack");
  ///<summary>vdi</summary>
  public static MimeEntry Vdi { get; } = new(["vdi"], "application/x-virtualbox-vdi");
  ///<summary>vhd</summary>
  public static MimeEntry Vhd { get; } = new(["vhd"], "application/x-virtualbox-vhd");
  ///<summary>vmdk</summary>
  public static MimeEntry Vmdk { get; } = new(["vmdk"], "application/x-virtualbox-vmdk");
  ///<summary>xsd</summary>
  public static MimeEntry Xsd { get; } = new(["xml", "xsl", "xsd", "rng"], "application/xml");
  ///<summary>rng</summary>
  public static MimeEntry Rng { get; } = new(["xml", "xsl", "xsd", "rng"], "application/xml");
  ///<summary>heic</summary>
  public static MimeEntry Heic { get; } = new(["heic"], "image/heic");
  ///<summary>heics</summary>
  public static MimeEntry Heics { get; } = new(["heics"], "image/heic-sequence");
  ///<summary>heif</summary>
  public static MimeEntry Heif { get; } = new(["heif"], "image/heif");
  ///<summary>heifs</summary>
  public static MimeEntry Heifs { get; } = new(["heifs"], "image/heif-sequence");
  ///<summary>jp2</summary>
  public static MimeEntry Jp2 { get; } = new(["jp2", "jpg2"], "image/jp2");
  ///<summary>jpg2</summary>
  public static MimeEntry Jpg2 { get; } = new(["jp2", "jpg2"], "image/jp2");
  ///<summary>jpx</summary>
  public static MimeEntry Jpx { get; } = new(["jpx", "jpf"], "image/jpx");
  ///<summary>jpf</summary>
  public static MimeEntry Jpf { get; } = new(["jpx", "jpf"], "image/jpx");
  ///<summary>dds</summary>
  public static MimeEntry Dds { get; } = new(["dds"], "image/vnd.ms-dds");
  ///<summary>manifest</summary>
  public static MimeEntry Manifest { get; } = new(["appcache", "manifest"], "text/cache-manifest");
  ///<summary>coffee</summary>
  public static MimeEntry Coffee { get; } = new(["coffee", "litcoffee"], "text/coffeescript");
  ///<summary>litcoffee</summary>
  public static MimeEntry Litcoffee { get; } = new(["coffee", "litcoffee"], "text/coffeescript");
  ///<summary>jade</summary>
  public static MimeEntry Jade { get; } = new(["jade"], "text/jade");
  ///<summary>jsx</summary>
  public static MimeEntry Jsx { get; } = new(["jsx"], "text/jsx");
  ///<summary>less</summary>
  public static MimeEntry Less { get; } = new(["less"], "text/less");
  ///<summary>mdx</summary>
  public static MimeEntry Mdx { get; } = new(["mdx"], "text/mdx");
  ///<summary>ini</summary>
  public static MimeEntry Ini { get; } = new(["txt", "text", "conf", "def", "list", "log", "in", "ini"], "text/plain");
  ///<summary>shex</summary>
  public static MimeEntry Shex { get; } = new(["shex"], "text/shex");
  ///<summary>slim</summary>
  public static MimeEntry Slim { get; } = new(["slim", "slm"], "text/slim");
  ///<summary>slm</summary>
  public static MimeEntry Slm { get; } = new(["slim", "slm"], "text/slim");
  ///<summary>stylus</summary>
  public static MimeEntry Stylus { get; } = new(["stylus", "styl"], "text/stylus");
  ///<summary>styl</summary>
  public static MimeEntry Styl { get; } = new(["stylus", "styl"], "text/stylus");
  ///<summary>hbs</summary>
  public static MimeEntry Hbs { get; } = new(["hbs"], "text/x-handlebars-template");
  ///<summary>lua</summary>
  public static MimeEntry Lua { get; } = new(["lua"], "text/x-lua");
  ///<summary>mkd</summary>
  public static MimeEntry Mkd { get; } = new(["mkd"], "text/x-markdown");
  ///<summary>pde</summary>
  public static MimeEntry Pde { get; } = new(["pde"], "text/x-processing");
  ///<summary>sass</summary>
  public static MimeEntry Sass { get; } = new(["sass"], "text/x-sass");
  ///<summary>scss</summary>
  public static MimeEntry Scss { get; } = new(["scss"], "text/x-scss");
  ///<summary>ymp</summary>
  public static MimeEntry Ymp { get; } = new(["ymp"], "text/x-suse-ymp");
  ///<summary>yaml</summary>
  public static MimeEntry Yaml { get; } = new(["yaml", "yml"], "text/yaml");
  ///<summary>yml</summary>
  public static MimeEntry Yml { get; } = new(["yaml", "yml"], "text/yaml");
}