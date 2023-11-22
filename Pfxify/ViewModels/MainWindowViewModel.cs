using CommunityToolkit.Mvvm.Input;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Pfxify.BusinessObjects;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Pfxify.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private RelayCommand<IDataObject>? _dropCommand;
        private RelayCommand? _toPfxCommand;

        public ObservableCollection<ICryptographyObject> CryptographyObjects { get; init; } = new ObservableCollection<ICryptographyObject>();

        public ICommand DropCommand
        {
            get => _dropCommand ??= new RelayCommand<IDataObject>(DoDrop);
        }

        public ICommand ToPfxCommand
        {
            get => _toPfxCommand ??= new RelayCommand(ToPfx, CanToPfx);
        }

        private bool CanToPfx()
        {
            return CryptographyObjects.Count > 0;
        }

        public MainWindowViewModel()
        {
            CryptographyObjects.CollectionChanged += CryptographyObjects_CollectionChanged;
        }

        private void CryptographyObjects_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _toPfxCommand?.NotifyCanExecuteChanged();
        }

        private void ToPfx()
        {
            var store = new Pkcs12StoreBuilder().Build();

            var privateKey = (CryptographyObjects.FirstOrDefault(x => x is PrivateKeyCryptographyObject) as PrivateKeyCryptographyObject)?.PrivateKey;
            var certificates = CryptographyObjects
                .Where(x => x is CertificateCryptographyObject)
                .Select(x => new X509CertificateEntry((x as CertificateCryptographyObject).Certificate));

            if (privateKey != null)
            {
                store.SetKeyEntry("key", new AsymmetricKeyEntry(privateKey), certificates.ToArray());
            }
            else
            {
                foreach (var cert in certificates)
                {
                    store.SetCertificateEntry(cert.Certificate.SubjectDN.ToString(), cert);
                }
            }

            using var fileStream = new FileStream(@"C:\Users\Mathias\cert\export.pfx", FileMode.Create, FileAccess.Write);

            var passwordIntput = new PasswordInput("export.pfx");
            if (passwordIntput.ShowDialog() != true)
            {
                return;
            }

            store.Save(fileStream, passwordIntput.Password.ToCharArray(), new Org.BouncyCastle.Security.SecureRandom());

            MessageBox.Show("DONE");
        }

        private void DoDrop(IDataObject? data)
        {
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = (string[])data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                switch (Path.GetExtension(file))
                {
                    case ".pem":
                        ParsePemFile(file);
                        break;

                    case ".pfx":
                        ParsePfxFile(file);
                        break;
                    default:
                        MessageBox.Show("Invalid file type");
                        break;
                }

            }
        }

        private void ParsePfxFile(string file)
        {
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            var store = new Pkcs12StoreBuilder().Build();

            try
            {
                store.Load(fileStream, Array.Empty<char>());
            }
            catch (IOException)
            {
                var passwordInput = new PasswordInput(Path.GetFileName(file));
                if (passwordInput.ShowDialog().GetValueOrDefault(false) == false)
                {
                    return;
                }

                fileStream.Seek(0, SeekOrigin.Begin);
                store = new Pkcs12StoreBuilder().Build();
                store.Load(fileStream, passwordInput.Password.ToCharArray());
            }

            foreach (var alias in store.Aliases)
            {
                if (store.IsCertificateEntry(alias))
                {
                    CryptographyObjects.Add(new CertificateCryptographyObject
                    {
                        Certificate = store.GetCertificate(alias).Certificate
                    });
                }
                else if (store.IsKeyEntry(alias))
                {
                    CryptographyObjects.Add(new PrivateKeyCryptographyObject
                    {
                        PrivateKey = store.GetKey(alias).Key
                    });
                }
            }

        }

        private void ParsePemFile(string file)
        {
            FileStream certStream;
            StreamReader certStreamReader;
            PemReader certPemReader;

            certStream = File.OpenRead(file);
            certStreamReader = new StreamReader(certStream);
            certPemReader = new PemReader(certStreamReader, new PasswordFinder(Path.GetFileName(file)));
            var certPemObject = certPemReader.ReadPemObject();

            if (certPemObject.Type.Contains("PRIVATE KEY"))
            {
                certStream.Seek(0, SeekOrigin.Begin);
                var certObject = certPemReader.ReadObject();
                CryptographyObjects.Add(new PrivateKeyCryptographyObject
                {
                    PrivateKey = (AsymmetricKeyParameter)certObject
                });
            }
            else if (certPemObject.Type.Contains("CERTIFICATE"))
            {
                certStream.Seek(0, SeekOrigin.Begin);
                var certObject = certPemReader.ReadObject();
                CryptographyObjects.Add(new CertificateCryptographyObject
                {
                    Certificate = (X509Certificate)certObject
                });
            }
        }

        private class PasswordFinder : IPasswordFinder
        {
            private readonly string _currentFileName;

            public PasswordFinder(string currentFileName)
            {
                _currentFileName = currentFileName;
            }
            public char[] GetPassword()
            {
                var passwordInput = new PasswordInput(_currentFileName);
                if (passwordInput.ShowDialog() == true)
                {
                    return passwordInput.Password.ToCharArray();
                }

                return null;
            }
        }
    }
}
