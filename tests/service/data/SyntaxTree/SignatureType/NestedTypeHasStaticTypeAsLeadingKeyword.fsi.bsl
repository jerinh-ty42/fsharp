SigFile
  (ParsedSigFileInput
     ("/root/SignatureType/NestedTypeHasStaticTypeAsLeadingKeyword.fsi",
      QualifiedNameOfFile NestedTypeHasStaticTypeAsLeadingKeyword, [],
      [SynModuleOrNamespaceSig
         ([NestedTypeHasStaticTypeAsLeadingKeyword], false, AnonModule,
          [Types
             ([SynTypeDefnSig
                 (SynComponentInfo
                    ([], None, [], [A],
                     PreXmlDoc ((2,0), FSharp.Compiler.Xml.XmlDocCollector),
                     false, None, (2,5--2,6)),
                  ObjectModel
                    (Unspecified,
                     [NestedType
                        (SynTypeDefnSig
                           (SynComponentInfo
                              ([], None, [], [B],
                               PreXmlDoc ((3,16), FSharp.Compiler.Xml.XmlDocCollector),
                               false, None, (3,16--3,17)),
                            ObjectModel (Class, [], (4,20--5,23)), [],
                            (3,16--5,23),
                            { LeadingKeyword =
                               StaticType ((3,4--3,10), (3,11--3,15))
                              EqualsRange = Some (3,18--3,19)
                              WithKeyword = None }), (3,4--6,0))], (3,4--6,0)),
                  [], (2,5--6,0), { LeadingKeyword = Type (2,0--2,4)
                                    EqualsRange = Some (2,7--2,8)
                                    WithKeyword = None })], (2,0--6,0))],
          PreXmlDocEmpty, [], None, (2,0--6,0), { LeadingKeyword = None })],
      { ConditionalDirectives = []
        WarnDirectives = []
        CodeComments = [] }, set []))
