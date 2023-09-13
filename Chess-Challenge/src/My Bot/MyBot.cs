﻿using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
#if UCI
    public ulong nodes = 0;
#endif

    readonly int[] weights = new int[6169];
    readonly Move[] tt = new Move[1048576];

    public MyBot()
    {
        for (int i = 0; i < 6169;)
        {
            var packed = decimal.GetBits(packedWeights[i / 16]);
            int num = i % 16 * 6;
            uint adj = ((uint)packed[num / 32] >> (num % 32)) & 63;
            if (num == 30) adj += ((uint)packed[1] & 15) << 2;
            if (num == 60) adj += ((uint)packed[2] & 3) << 4;
            weights[i++] = (int)adj - 31;
        }
    }

#if UCI
    public Move Think(Board board, Timer timer) => ThinkInternal(board, timer);

    public Move ThinkInternal(Board board, Timer timer, int maxDepth = 50, bool report = true)
#else
    public Move Think(Board board, Timer timer)
#endif
    {
        Move bestMoveRoot = default;
        var killers = new Move[128];

#if UCI
        nodes = 0;
        for (int depth = 0; ++depth <= maxDepth;)
#else
        for (int depth = 0; ++depth <= 50;)
#endif
        {
#if UCI
            int score =
#endif
            Search(-30000, 30000, depth, 0);

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;

#if UCI
            if (report)
            {
                ulong time = (ulong)timer.MillisecondsElapsedThisTurn;
                ulong nps = nodes * 1000 / Math.Max(time, 1);
                Console.WriteLine(
                    $"info depth {depth} score cp {score} time {time} nodes {nodes} nps {nps}"
                );
            }
#endif
        }

        return bestMoveRoot;

        int Search(int alpha, int beta, int depth, int ply)
        {
            bool qs = depth <= 0, root = ply == 0;
#if UCI
            nodes++;
#endif
            ulong key = board.ZobristKey % 1048576, moveIdx = 0;

            if (!root && board.IsRepeatedPosition())
                return 0;

            if (qs && (alpha = Math.Max(alpha, Evaluate())) >= beta)
                return alpha;

            var moves = board.GetLegalMoves(qs);

            if (!qs && moves.Length == 0)
                return board.IsInCheck() ? ply - 30_000 : 0;

            var scores = new int[moves.Length];
            foreach (Move move in moves)
            {
                scores[moveIdx++] = move == tt[key]
                    ? -1000000
                    : move.IsCapture
                        ? (int)move.MovePieceType - 100 * (int)move.CapturePieceType
                        : move == killers[ply]
                            ? 500000
                            : 1000000;
            }

            Array.Sort(scores, moves);

            tt[key] = default;

            foreach (Move move in moves)
            {
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                    return 30000;

                board.MakeMove(move);
                int score = -Search(-beta, -alpha, depth - 1, ply + 1);
                board.UndoMove(move);

                if (score > alpha)
                {
                    alpha = score;
                    tt[key] = move;
                    if (root) bestMoveRoot = move;
                    if (alpha >= beta) {
                        if (!move.IsCapture)
                            killers[ply] = move;
                        break;
                    }
                }
            }

            return alpha;
        }

        int Evaluate()
        {
            var accumulators = new int[2, 8];

            void updateAccumulator(int side, int feature)
            {
                for (int i = 0; i < 8;)
                    accumulators[side, i] += weights[feature * 8 + i++];
            }

            updateAccumulator(0, 768);
            updateAccumulator(1, 768);

            for (int stm = 2; --stm >= 0;)
                for (var p = 0; p <= 5; p++)
                    for (ulong mask = board.GetPieceBitboard((PieceType)p + 1, stm > 0); mask != 0;)
                    {
                        int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
                        updateAccumulator(0, 384 - stm * 384 + p * 64 + sq);
                        updateAccumulator(1, stm * 384 + p * 64 + (sq ^ 56));
                    }

            int eval = weights[6168];

            for (int i = 0; i < 16;)
                eval += Math.Clamp(accumulators[i / 8 ^ (board.IsWhiteToMove ? 0 : 1), i % 8], 0, 32) * weights[6152 + i++];


            return eval * 400 / 1024;
        }
    }

    readonly decimal[] packedWeights = {
        38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,39043314753596443297281071264m,39081702868520201031740672095m,39081698444795572431385377119m,45289555917781785543762115101m,
        39043314827419448319361800352m,39062053173048376264613021855m,41518595313092894469403560351m,45270208382301472867895601692m,39062973890415366771247003873m,39023669781734662174119156000m,39042710438350941830515378529m,45270199011355483493236595104m,
        37805690670234183664964573408m,39042719735511358027898669282m,39042398834780511335723944165m,44031937848789368612697528608m,36645130662883673247360583967m,32911359358314127467443493031m,37803844815236603486117145002m,42813019724210846520489388453m,
        30339987627986443161401894621m,33975516026365504406993082470m,38940253824271088511881951846m,42671292125908572424435255914m,38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,
        31612921746130140518272342166m,31652211982842140783260596695m,30432693967821908041266485655m,26622447121186810662388877783m,32889240385170023619591535062m,31690590654095496351933917656m,29195669997360317367043611097m,25441619658792087985719728730m,
        31670629066857227081852762519m,29253089250255437973696137560m,29234048741194247753137252890m,30452353182931689050914814489m,32947259455872228416021457432m,30510674332939191815581059547m,29272436861792827029585779229m,29234058263155498454433519899m,
        31710221608793646639741270361m,30491322152320395562072799709m,30530626629936817125898447197m,30472305404989983912957442331m,31671224014634643914204772696m,29272720131130855847472527836m,28074084424174374509917116831m,30510995679760570995370494171m,
        32967201789358521185910266139m,30510669469879961849045051483m,30490727210182607239908817117m,32888952546753692828932893155m,32928525459082074462994429916m,30509757977056525051620096352m,31825385370980662102955530330m,34087290601496780869648115868m,
        25500569699773038858911467543m,27995197428049296124646115414m,29232844534208864245705980054m,27917556779343217125712853141m,22947322880146817365183674390m,25462493117067230190984595414m,24224250920131088062624143573m,21708778374583227660852115668m,
        26777199797135223062452893782m,28034487516016104157760571479m,28015149426456863235369187479m,26757266688246496340042180822m,26739109485385236587342309529m,23044037162502394264457943128m,24281679547448700085511485464m,24262648634171774314645653592m,
        26776902360717167471282997337m,29272115806480408996880169113m,26776600202994911110019008603m,30490722627300459604632795289m,23004454140239124249274320857m,21747459500869040142167148568m,20568454744017595957868861401m,24262629523291433642591090648m,
        29232542077881072814352885910m,27994295304983153348440606936m,28014525926472832036363687900m,29291453972149076108318984153m,23023499515728496374858475481m,24280494601190824019032885209m,25518108502004393621520480283m,25441629850619037872408319960m,
        26814983446986047627873053458m,25615129295618233575581252370m,23159187123260867765294176850m,25575244258614672620773530010m,28072280318874724794919115668m,26853678444123109131798280916m,25615743422334946930170620628m,28109162143960961530990525015m,
        26872723821918333129226808020m,28168994608110953169417759446m,25654428974775920450074671828m,28109761889151723854035782230m,26911711752270984247811074836m,26931351928052212551563184853m,25693421555995911151159708438m,26891471913174439724568648471m,
        26911706958441668432777542357m,25673460040275085440812419798m,25653824662953155045838129943m,26910809931306027606944357078m,26872709803564050158684310228m,24416167815741208290367212312m,25634783932531603014892759832m,26891769349647382932993452824m,
        25654410086481769301654997590m,24435510629989945065196587737m,24416479640674238857554266839m,26891764703355705038683347673m,25654122244621301142833695444m,25634769692797818073120090903m,25634477126301902000106089174m,28110055047168424845660498710m,
        12130710423331087256877669578m,10912703491798954986496978185m,10912103744249159885908720777m,14604781906877338872580757840m,12149434605264729559328722120m,10951398563894773108961037448m,10950487292396513339580976201m,10950501749817411236553919179m,
        12188729492861502851925781703m,12208081818694924248400370825m,10989484591334704816674933899m,13426674393874441857367382987m,14722042916505230624748838025m,11047819913071070800829840522m,11028170294808962718282898444m,13484995625738509501881220046m,
        15959978228812446544947694733m,13523090729084343441015532685m,12305102839222077406757096463m,12227717495688054315783134225m,14720522240656450164820642892m,12283634888556910193036586060m,12305707525762904760749501520m,12286057687328540886263770066m,
        14739567541205651880728185742m,11026965941476035285757828175m,11028769962719639955058826446m,14703602297742199031674813391m,17196700049659126805306383310m,11047206161019225224268336076m,13542721763351515806514138061m,17294939446431678345467251604m,
        39004312952319051593785644510m,42660727835761880607679186591m,38946624524679665755068434399m,32737274708855727339135182816m,39004610234335996968865228190m,42738401244751081764470777696m,38985607660014064293705013282m,36509732527476531981580109920m,
        42738689160342512353077471006m,45291647780812111306255103968m,41500182660836599228916693153m,37747984093087727843625806048m,45253254793696122558354151391m,44034969321030248683910596769m,40262832994625051324197771490m,37786962435745713932024031521m,
        45252324118564683178527561632m,45233915748419560460087077087m,40263423291624641672217499994m,39045128368408532257822620004m,43955750367658519529174726561m,45156832852006295220830738591m,42701524289603041952878958931m,40264018459680988441101343136m,
        46411300218086211086331566108m,45097878387249161365933934685m,43900457284268719111724730584m,42682460024630836455234476382m,45077831948374564658469549981m,43666151022958904160029124574m,47595514924732224250735888538m,43648953048516544682749987159m,
        38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,58794825311889201079599903718m,71176317633669476385852126056m,59957080352849951498915448812m,57402577442445093277501212968m,
        51367473284773360498323904038m,50129202454025400478260688424m,55080915761083004720169506795m,53823288024896702290964682919m,46434427493717593138525263526m,45234856678701665101608277669m,51385843064598403886660605926m,50089851047907241990286108901m,
        42739945614200766103231813285m,44016254731333991456743241574m,46453435091534426027688073190m,48851325361390723710461302884m,41463910463534862005219518181m,40263135151069679508682586085m,43957909892283799243610535908m,48870370516707272892583127138m,
        42701850430185910344238134947m,38986811720452003261472724835m,43938269570117296040519280610m,47651773291688362246269442146m,38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,38985286316542769292061308895m,
        43822240287421736791420343398m,43802873642339806807997440936m,46181345915847339502794048424m,46317718122440870845198197864m,45118808438696797716668380393m,45041441829002035489303835753m,50089613672720466682361816487m,46318346043864253444163282408m,
        48871932811601189375913907367m,50013153985759222957245351208m,47478328957435670188729185770m,50031561696503341036001965480m,45079830245936903244459693418m,47478641153626872155597063592m,47498276605888691341305464293m,50050923474023562657852966373m,
        45099772874551678940364297511m,46299027210363100379794523558m,47517619566629962712283235813m,48813573878157664856164144421m,46357650963030185374843286822m,46279986701355953424506434982m,45080415738711739260398093798m,45118794487192262892397929958m,
        46357348730385469400955901351m,43862125689974018076122957222m,43881468429355204846855276005m,38907630903273152019330119334m,42720890201178616143368308389m,42682521121097548880938161574m,41404365254333718993964508516m,37710496918063840350951318884m,
        41364489522613655829501572453m,42564041670678373464522994980m,42544368364527069317475718311m,42583984219795907948861314214m,47591680139179049622698099878m,47515508438120568426132821351m,48773089098214873249707353319m,48812058214077237803846599976m,
        43840383768302451212249068774m,47593494051407195924479997223m,48773698434596509807759197414m,45020285923441600482996516006m,47535163228406842532773127594m,48753760230875587705474794982m,46277582643215843212973196775m,48812086400702762908834227750m,
        45059883188949770038851270058m,45020899830020956613447644581m,46278475472618606445789836709m,47575043765594367008210814309m,47555412807348510582773286310m,46316872878792082878059918759m,48792743512684488231005079078m,48792734144025768844276676135m,
        46318063217230631009111115112m,46336806290953776192821253478m,45078918971068534434937042405m,45097652598906372984451757542m,46393333005034707278286435689m,46355539911855495450125038886m,46394532346635310025057130086m,47612534331000942747927590373m,
        53631473922455682861041116075m,53612442711779056705670415471m,54850373306349204164795110573m,53612140625610277026241377451m,53631483588568726786621768750m,53612140552976508107993838767m,54850075722321448527638230189m,56107065935608992209974002991m,
        53631781097674876099740513326m,53651128559403682590588601519m,58583541032320279597398820143m,54849783158149389794701673839m,53631483883734356092325139438m,53632083402934493305838961838m,56107651656626257685704533358m,53631176562220808910538212718m,
        52432220024791038250798748719m,53631479012677036722598384943m,54850359508239243244219336046m,51155594139123467037302265262m,51174025608068658677032883183m,52393529454871709944623330480m,52374181920561761563282437359m,51155886853174758418084717871m,
        53669243628642163542530397037m,53651114463767629942443668720m,54888747546944081022482572590m,52433112405687623806678660525m,51173114042573717587976713132m,52393227074653617589558895597m,52412867471849531966163969326m,47500090302377799267506910573m,
        64484051931790164200795394297m,65723512498233220155727921784m,66902205653783507237328622136m,65683306198337630476607938936m,64503701920122910036697006779m,66960546063344441062968771259m,66960545843046924641507201721m,68198197964361289202957529147m,
        64504013448845185911381017274m,66980795498225241720865137274m,68160107357499421746660885053m,64465318301629611603613176059m,63304749590945863159758967610m,66941505407880238486199710329m,66922460178794441461727736440m,63227982652691205819294527992m,
        62047164507128055429511754232m,65723810226376432356827046455m,64485260930333918747865879158m,62009385572958705093679231542m,63323785525177654341945237880m,64504599018738544916652851768m,64504603813757382916781867575m,64543289291240444032556434040m,
        62104871969591303215682376247m,63305646837136299724007514614m,64542991929744271913154486902m,57155236649142649885795508917m,63380897675487913717721182585m,64619744412649972646984274422m,64639701127696573992091759222m,59611178902169037576106885817m,
        38985624113409461573455833693m,38985625803502895740373551198m,41501045970710468038721660895m,42680300412220530993120016286m,41462718211443367232331966876m,38986234994562089323275614366m,42700593169337205115700512735m,42699927235876513873407383261m,
        41424638744055592845667334494m,40224751682562007072803002655m,42681874448774720199864494111m,43976591130159814843458791071m,40225063284980919036553074975m,41327580247744563902641744031m,42681274856958265382456936543m,42680028371262164008996054943m,
        38986514431641945333926865182m,38967162249870236372979566752m,41461761566646954660045936672m,41461133416964844060385097695m,37728929494208845226943043736m,38985895950341553567542433887m,40223217064433743940381460447m,42698181000465800586699089951m,
        37747955679336397924724463834m,38985593718868635581565245599m,39023367482148740265466017760m,41459947948468416105699731423m,35272382398924593040173369500m,39062969544999886496949450847m,39042408136459740207305070690m,41478393431264295490927835103m,
        1199258990120150323122791890m,12094695517970494m,
    };
}