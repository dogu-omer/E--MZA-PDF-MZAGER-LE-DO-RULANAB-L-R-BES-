Copilot said:
5070 Sayılı Elektronik İmza Kanununa Uygun Toplu PDF İmzalama Uygulaması Geliştirilmesi
Giriş

Dijital dönüşüm süreçlerinin hız kazanmasıyla birlikte elektronik belgelerin güvenli bir şekilde imzalanması ve doğrulanması kurumlar açısından kritik bir ihtiyaç haline gelmiştir. Özellikle yüksek sayıda PDF dokümanının kısa sürede imzalanması gereken senaryolarda toplu imzalama çözümleri önemli avantajlar sağlamaktadır. Bu kapsamda C# programlama dili kullanılarak geliştirilen uygulamamızın temel amacı, PDF dosyalarının 5070 sayılı Elektronik İmza Kanunu'na uygun şekilde elektronik olarak imzalanması ve oluşturulan imzaların İmzager gibi doğrulama yazılımları tarafından eksiksiz doğrulanabilmesidir.

5070 sayılı Elektronik İmza Kanunu elektronik imzanın hukuki ve teknik çerçevesini belirlemektedir. Kanunun 1. maddesinde, elektronik imzanın hukuki ve teknik yönleri ile kullanım esaslarının düzenlenmesinin amaçlandığı belirtilmektedir.

Elektronik İmzanın Hukuki Dayanağı

5070 sayılı Kanunun en önemli hükümlerinden biri güvenli elektronik imzanın hukuki geçerliliğini tanımlayan 5. maddesidir. Kanunda açıkça:

"Güvenli elektronik imza, elle atılan imza ile aynı hukukî sonucu doğurur."

hükmü yer almaktadır.

Aynı kanunun 4. maddesinde ise güvenli elektronik imzanın özellikleri aşağıdaki şekilde tanımlanmıştır:

"Güvenli elektronik imza; münhasıran imza sahibine bağlı olan, sadece imza sahibinin tasarrufunda bulunan güvenli elektronik imza oluşturma aracı ile oluşturulan, nitelikli elektronik sertifikaya dayanarak imza sahibinin kimliğinin tespitini sağlayan ve imzalanmış elektronik veride sonradan herhangi bir değişiklik yapılıp yapılmadığının tespitini sağlayan elektronik imzadır."

Bu hükümler, geliştirilen uygulamanın yalnızca teknik olarak değil, aynı zamanda hukuki açıdan da geçerli imzalar üretmesini zorunlu kılmaktadır.

Geliştirilen Uygulamanın Amacı

C# dili kullanılarak geliştirilen uygulama, çok sayıdaki PDF dosyasının tek işlemle imzalanmasını hedeflemektedir. Geleneksel yöntemlerde her PDF dosyasının ayrı ayrı seçilip imzalanması zaman kaybına neden olurken, toplu imzalama yaklaşımı operasyonel verimliliği önemli ölçüde artırmaktadır.

Uygulamanın temel özellikleri şunlardır:

Bir klasörde bulunan çok sayıdaki PDF dosyasını otomatik olarak tespit etme,
Elektronik imza sertifikasını güvenli şekilde kullanma,
PDF dosyalarına PAdES uyumlu elektronik imza ekleme,
İmza sonrası belge bütünlüğünü koruma,
İmzalanan belgelerin İmzager doğrulama yazılımı tarafından doğrulanabilmesini sağlama,
Toplu işlem sırasında detaylı log kayıtları oluşturma.

Bu sayede kurumlar binlerce dokümanı kısa süre içerisinde yasal geçerliliğe sahip elektronik imza ile imzalayabilmektedir.

Teknik Yaklaşım

Uygulama Microsoft .NET platformu ve C# dili ile geliştirilmiştir. Elektronik imza işlemleri sırasında nitelikli elektronik sertifikalar kullanılmaktadır. PDF dosyasının kriptografik özeti (hash değeri) oluşturulmakta, bu özet imza sahibinin özel anahtarı ile imzalanmakta ve sonuç PDF içerisine gömülmektedir.

Süreç genel olarak aşağıdaki adımlardan oluşmaktadır:

PDF dosyalarının okunması,
İmzalanacak alanların belirlenmesi,
Sertifika bilgilerinin yüklenmesi,
Kriptografik imza oluşturulması,
İmzanın PDF içerisine eklenmesi,
İmzalı dokümanın kaydedilmesi,
İmzager ile doğrulama yapılması.

Bu yaklaşım sayesinde belge üzerinde daha sonra yapılabilecek herhangi bir değişiklik doğrulama aşamasında tespit edilebilmektedir. Bu özellik, 5070 sayılı Kanunda belirtilen "imzalanmış elektronik veride sonradan herhangi bir değişiklik yapılıp yapılmadığının tespit edilmesi" şartını da desteklemektedir.

İmzager ile Doğrulama

Elektronik imza uygulamalarında en önemli konulardan biri üretilen imzanın bağımsız doğrulama araçları tarafından doğrulanabilmesidir. Bu nedenle geliştirilen sistem tasarımında İmzager doğrulama yazılımı temel referanslardan biri olarak değerlendirilmiştir.

İmzalama işlemi tamamlandıktan sonra PDF dosyası İmzager üzerinde açıldığında;

Sertifika geçerliliği,
İmza sahibinin bilgileri,
İmzalama zamanı,
Sertifika zinciri,
Belge bütünlüğü,

başarıyla doğrulanabilmektedir. Böylece imzalanan dokümanın hem teknik hem de hukuki açıdan güvenilir olduğu gösterilmektedir.

Sonuç

Elektronik belge yönetim sistemlerinin yaygınlaşması, güvenli ve yasal geçerliliği bulunan dijital imza çözümlerine olan ihtiyacı artırmıştır. C# dili kullanılarak geliştirilen toplu PDF imzalama uygulaması, bu ihtiyaca cevap veren modern bir çözüm sunmaktadır. Uygulama, 5070 sayılı Elektronik İmza Kanunu'nun öngördüğü güvenli elektronik imza prensiplerine uygun olarak çalışmakta ve oluşturulan imzaların İmzager doğrulama programı ile kontrol edilmesine imkan sağlamaktadır.

Sonuç olarak sistem; zaman tasarrufu, operasyonel verimlilik, belge bütünlüğünün korunması ve hukuki geçerlilik açısından kurumlara önemli katkılar sağlamaktadır. Özellikle yüksek hacimli doküman yönetimi gerektiren kamu kurumları, üniversiteler ve özel sektör kuruluşları için toplu elektronik imzalama çözümleri dijital dönüşüm sürecinin vazgeçilmez bileşenlerinden biri haline gelmiştir.

Kaynakça
5070 Sayılı Elektronik İmza Kanunu, Resmî Gazete, 23.01.2004.
Elektronik İmza Kanunu Tam Metni, Mevzuat Bilgi Sistemi.
