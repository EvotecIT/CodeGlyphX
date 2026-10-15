"""Validate actual square-filter outputs and independently owned DPM retries."""
import hashlib
import itertools


def digest(value):
    return hashlib.sha256(value).hexdigest().upper()


def is_digest(value):
    return isinstance(value, str) and len(value) == 64 and all(c in '0123456789ABCDEF' for c in value)


def identity(data, proof, cases):
    if (data['Status'] != 'Passed' or data['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or data['CandidateCoreSha256'] != proof['CoreSha256'] or data['Cases'] != cases
            or len(data['Records']) != cases or [r['Id'] for r in data['Records']] != list(range(cases))):
        raise ValueError('DPM actual-output count, order or binary identity differs')


def validate_morphology(data, proof):
    identity(data, proof, 19240)
    if (data['Inputs'] != 962 or data['QrClippedCases'] != 11544 or data['DpmZeroOutsideCases'] != 7696
            or not data['DirtyIntegerPoolSeededBeforeEveryInput'] or not data['SourceAndDestinationGuardsPreserved']
            or proof['MorphologyEquivalence']['Cases'] != data['Cases']):
        raise ValueError('Square-filter policy coverage or guards differ')
    shapes = ((1,1),(1,2),(2,1),(1,3),(3,1),(2,2),(2,3),(3,2),(3,3))
    boundaries = ((1,4097),(4097,1),(32,128),(128,32),(257,3),(3,257))
    expected = {f'exhaustive/{w}/{h}/{v}' for w,h in shapes for v in range(1 << (w*h))}
    expected |= {f'seeded/{i}' for i in range(256)}
    expected |= {f'boundary/{w}/{h}/{p}' for w,h in boundaries for p in range(4)}
    groups = {}
    for row in data['Records']:
        groups.setdefault(row['InputId'], []).append(row)
    if set(groups) != expected:
        raise ValueError('Square-filter input coverage differs')
    for key, rows in groups.items():
        first = rows[0]; w,h = first['Width'],first['Height']; count=w*h
        if type(w) is not int or type(h) is not int or min(w,h) < 1:
            raise ValueError('Square-filter dimensions differ')
        combinations = {(z,r,e) for z in (False,True) for r in ((0,1,2,3) if z else (0,1,2,3,12,128)) for e in (False,True)}
        if len(rows) != 20 or {(r['ZeroOutside'],r['Radius'],r['Erode']) for r in rows} != combinations:
            raise ValueError('Square-filter radius or border policy coverage differs')
        bits = None; parts=key.split('/')
        if parts[0] == 'exhaustive':
            ew,eh,v=map(int,parts[1:]);bits=bytes((v>>i)&1 for i in range(ew*eh))
            if (w,h)!=(ew,eh): raise ValueError('Exhaustive filter dimensions differ')
        elif parts[0] == 'boundary':
            ew,eh,p=map(int,parts[1:]);bits=bytes(0 if p==0 else 255 if p==1 else i%2 if p==2 else (i*73+i//w*19+31)%256 for i in range(ew*eh))
            if (w,h)!=(ew,eh): raise ValueError('Boundary filter dimensions differ')
        elif max(w,h)>64:
            raise ValueError('Seeded filter dimensions differ')
        if not is_digest(first['InputSha256']) or (bits is not None and digest(bits)!=first['InputSha256']):
            raise ValueError('Square-filter input digest differs')
        for row in rows:
            prefix=row['OutputPrefixHex']
            if (row['Width']!=w or row['Height']!=h or row['InputSha256']!=first['InputSha256']
                    or type(row['Erode']) is not bool or type(row['ZeroOutside']) is not bool
                    or type(row['Radius']) is not int or not is_digest(row['OutputSha256'])
                    or row['PreviousOutputSha256']!=row['OutputSha256']
                    or len(prefix)!=2*min(count,32) or any(c not in '0123456789ABCDEF' for c in prefix)
                    or any(b not in (0,1) for b in bytes.fromhex(prefix))
                    or (count<=32 and digest(bytes.fromhex(prefix))!=row['OutputSha256'])):
                raise ValueError('Actual square-filter bytes or preserved input differ')


def validate_variants(data, proof, manifest):
    identity(data,proof,3414)
    if (data['Inputs']!=93 or data['SyntheticInputs']!=84 or data['FrozenImages']!=9
            or data['SyntheticOptionCases']!=20 or data['Variants']!=14886 or data['LifecycleCases']!=17
            or len(data['Lifecycle'])!=17 or not data['DirtyByteAndIntegerPoolsSeededBeforeEveryCase']
            or proof['VariantEquivalence']['Cases']!=data['Cases']):
        raise ValueError('DPM retry coverage or dirty-pool qualification differs')
    profiles=('Auto','DotPeen','LaserEtch'); adaptive=(0,1,2,128);bias=(0,4,64)
    options={(profile,adaptive[(p+r)%4],r,bias[(p+r)%3],6 if r%2==0 else 8) for p,profile in enumerate(profiles) for r in range(4)}
    options|={('Auto',2,1,4,a) for a in range(1,9)}
    frozen_options={('Auto',0,1,4,6),('DotPeen',0,1,2,6),('LaserEtch',0,0,3,6)}
    shapes=((1,1),(1,2),(2,1),(3,3),(9,7),(23,17),(257,3),(3,257),(129,127),(65,33),(4097,1),(1,4097))
    synthetic={f'synthetic/{w}/{h}/{p}' for w,h in shapes for p in range(7)}
    frozen={r['Id']:r for r in manifest['Samples']}
    groups={}; variants=0
    for row in data['Records']:groups.setdefault(row['InputId'],[]).append(row)
    if len(frozen)!=9 or set(groups)!=synthetic|set(frozen):raise ValueError('DPM retry input coverage differs')
    keys=('Profile','AdaptiveRadius','MorphologyRadius','Bias','Attempts')
    for key,rows in groups.items():
        expected=options if key in synthetic else frozen_options
        if len(rows)!=2*len(expected) or {(tuple(r['Options'][k] for k in keys),r['Mode']) for r in rows}!=set(itertools.product(expected,('retain','mutate'))):
            raise ValueError('DPM retry settings or ownership-mode coverage differs')
        first=rows[0];w,h=first['Width'],first['Height'];input_hash=first['PixelInputSha256']
        if not is_digest(input_hash) or type(w) is not int or type(h) is not int or min(w,h)<1:
            raise ValueError('DPM retry input shape differs')
        if key in synthetic:
            ew,eh,p=map(int,key.split('/')[1:]);pixels=bytearray()
            for i in range(ew*eh):
                gray=(0,1,2,127,255)[p] if p<5 else (i*73+i//ew*19+31)%256
                pixels.extend((gray,gray,gray,(0,1,127,255)[i%4] if p==6 else 255))
            if (w,h)!=(ew,eh) or digest(pixels)!=input_hash:raise ValueError('Synthetic retry pixels differ')
        for row in rows:
            setting=row['Options'];count=min(setting['Attempts'],6 if setting['Profile'] in ('Auto','DotPeen') and setting['MorphologyRadius']>0 else 3)
            hashes=row['PixelHashes'];variants+=len(hashes)
            retained=hashes if row['Mode']=='retain' else [digest(bytes([73])*(w*h))]*len(hashes)
            if (row['Width']!=w or row['Height']!=h or row['PixelInputSha256']!=input_hash
                    or row['FileSha256']!=(None if key in synthetic else frozen[key]['Sha256'].upper())
                    or len(hashes)!=count or any(not is_digest(v) for v in hashes)
                    or row['PreviousPixelHashes']!=hashes or row['RetainedPixelHashes']!=retained
                    or row['WorkspaceBufferSlots']!=6 or not row['PrivateBuffersCleared']):
                raise ValueError('DPM retry pixels, attempt count or independent ownership differs')
    if variants!=14886:raise ValueError('DPM retry image count differs')
    expected={(mode,n) for mode in ('cancel','break','fault') for n in (range(7) if mode=='cancel' else range(1,6))}
    if {(r['Mode'],r['StopAfter']) for r in data['Lifecycle']}!=expected:
        raise ValueError('DPM cancellation, disposal or exception coverage differs')
    for row in data['Lifecycle']:
        hashes=row['PixelHashes'];count=row['StopAfter']
        if (len(hashes)!=count or any(not is_digest(v) for v in hashes) or row['PreviousPixelHashes']!=hashes
                or row['RetainedPixelHashes']!=hashes or row['WorkspaceBufferSlots']!=(6 if count else 0)
                or not row['PrivateBuffersCleared']):
            raise ValueError('DPM partial retries or disposed private buffers differ')
